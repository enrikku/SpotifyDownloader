using Newtonsoft.Json;
using SpotifyPlayListDownloader.Clases;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using log4net;

namespace SpotifyPlayListDownloader.Services
{
    public class SpotifyService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SpotifyService));

        private readonly string _clientId = "";
        private readonly string _clientSecret = "";

        public SpotifyService(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            Log.Info("SpotifyService initialized");
        }

        public async Task<string> GetAccessTokenAsync()
        {
            Log.Info("Requesting Spotify access token...");
            try
            {
                using var client = new HttpClient();
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
                {
                    Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "client_credentials")
                    })
                };

                var response = await client.SendAsync(request);
                Log.Debug($"Access token response status: {response.StatusCode}");

                var json = await response.Content.ReadAsStringAsync();
                var token = JsonDocument.Parse(json).RootElement.GetProperty("access_token").GetString();

                Log.Info("Spotify access token retrieved successfully");
                return token;
            }
            catch (Exception ex)
            {
                Log.Error("Error retrieving access token", ex);
                throw;
            }
        }

        public async Task<PlayListTracks.Root> GetPlaylistTracksAsync(string playlistId, string accessToken)
        {
            Log.Info($"Retrieving playlist tracks for playlistId={playlistId}");
            try
            {
                var playlistInfoUrl = $"https://api.spotify.com/v1/playlists/{playlistId}";
                var tracksUrl = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?limit=100";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Obtener info básica de la playlist
                Log.Debug($"Requesting playlist info: {playlistInfoUrl}");
                var response = await client.GetAsync(playlistInfoUrl);
                response.EnsureSuccessStatusCode();
                var playlistJson = await response.Content.ReadAsStringAsync();
                Log.Debug("Playlist info retrieved");

                var playlistRoot = JsonConvert.DeserializeObject<PlayListTracks.Root>(playlistJson);
                playlistRoot.tracks.items = new List<PlayListTracks.Item>();

                string nextUrl = tracksUrl;

                // Obtener todas las páginas de tracks
                while (!string.IsNullOrEmpty(nextUrl))
                {
                    Log.Debug($"Requesting tracks page: {nextUrl}");
                    var pageResponse = await client.GetAsync(nextUrl);
                    pageResponse.EnsureSuccessStatusCode();

                    var pageJson = await pageResponse.Content.ReadAsStringAsync();
                    var trackPage = JsonConvert.DeserializeObject<PlayListTracks.Tracks>(pageJson);

                    if (trackPage?.items != null)
                    {
                        Log.Debug($"Retrieved {trackPage.items.Count} tracks");
                        playlistRoot.tracks.items.AddRange(trackPage.items);
                    }
                    else Log.Warn("No tracks found in this page");

                    nextUrl = trackPage.next;
                }

                Log.Info($"Playlist tracks retrieval completed. Total tracks: {playlistRoot.tracks.items.Count}");
                return playlistRoot;
            }
            catch (Exception ex)
            {
                Log.Error($"Error retrieving playlist tracks for playlistId={playlistId}", ex);
                return null;
            }
        }
    }
}
