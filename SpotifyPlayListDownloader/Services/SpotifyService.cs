using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SpotifyPlayListDownloader.Clases;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration.Json;

namespace SpotifyPlayListDownloader.Services
{
    public class SpotifyService
    {
        private readonly string _clientId = "9e73c2b1e6f442a1ae1af9351096541a";
        private readonly string _clientSecret = "a2233b827a3d4521974219030476be45";

        public SpotifyService(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            using var client = new HttpClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            })
            };

            var response = await client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(json).RootElement.GetProperty("access_token").GetString();
        }

        //public async Task<PlayListTracks.Root> GetPlaylistTracksAsync(string playlistId, string accessToken)
        //{
        //    var url = $"https://api.spotify.com/v1/playlists/{playlistId}";

        //    using var client = new HttpClient();
        //    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        //    var response = await client.GetAsync(url);
        //    response.EnsureSuccessStatusCode();

        //    var json = await response.Content.ReadAsStringAsync();

        //    var playlistRoot = JsonConvert.DeserializeObject<PlayListTracks.Root>(json);
        //    return playlistRoot;
        //}

        public async Task<PlayListTracks.Root> GetPlaylistTracksAsync(string playlistId, string accessToken)
        {
            try
            {
                var playlistInfoUrl = $"https://api.spotify.com/v1/playlists/{playlistId}";
                var tracksUrl = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?limit=100";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Obtener info básica de la playlist
                var response = await client.GetAsync(playlistInfoUrl);
                response.EnsureSuccessStatusCode();
                var playlistJson = await response.Content.ReadAsStringAsync();
                var playlistRoot = JsonConvert.DeserializeObject<PlayListTracks.Root>(playlistJson);

                // Limpiar lista de items (llenaremos manualmente)
                playlistRoot.tracks.items = new List<PlayListTracks.Item>();

                string nextUrl = tracksUrl;

                while (!string.IsNullOrEmpty(nextUrl))
                {
                    var pageResponse = await client.GetAsync(nextUrl);
                    pageResponse.EnsureSuccessStatusCode();

                    var pageJson = await pageResponse.Content.ReadAsStringAsync();
                    var trackPage = JsonConvert.DeserializeObject<PlayListTracks.Tracks>(pageJson);

                    if (trackPage?.items != null)
                    {
                        playlistRoot.tracks.items.AddRange(trackPage.items);
                    }

                    nextUrl = trackPage.next;
                }

                return playlistRoot;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
    }
}