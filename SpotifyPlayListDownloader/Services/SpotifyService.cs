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
            Log.Info("SpotifyService iniciado");
        }

        public async Task<string> GetAccessTokenAsync()
        {
            Log.Info("Obteniendo token de acceso...");
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
                Log.Debug($"Respuesta al obtener token de acceso: {response.StatusCode}");

                var json = await response.Content.ReadAsStringAsync();
                var token = JsonDocument.Parse(json).RootElement.GetProperty("access_token").GetString();

                Log.Info("Token de acceso obtenido correctamente");
                return token;
            }
            catch (Exception ex)
            {
                Log.Error("Error al obtener el token de acceso", ex);
                throw;
            }
        }

        public async Task<PlayListTracks.Root> GetPlaylistTracksAsync(string playlistId, string accessToken)
        {
            Log.Info($"Obteniendo canciones de la playlist con ID={playlistId}");
            try
            {
                var playlistInfoUrl = $"https://api.spotify.com/v1/playlists/{playlistId}";
                var tracksUrl = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?limit=100";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Obtener info básica de la playlist
                Log.Debug($"Obteniendo información de la playlist: {playlistInfoUrl}");
                var response = await client.GetAsync(playlistInfoUrl);
                response.EnsureSuccessStatusCode();
                var playlistJson = await response.Content.ReadAsStringAsync();
                Log.Debug("Información de la playlist obtenida");

                var playlistRoot = JsonConvert.DeserializeObject<PlayListTracks.Root>(playlistJson);
                playlistRoot.tracks.items = new List<PlayListTracks.Item>();

                string nextUrl = tracksUrl;

                // Obtener todas las páginas de canciones
                while (!string.IsNullOrEmpty(nextUrl))
                {
                    Log.Debug($"Obteniendo canciones de la playlist: {nextUrl}");
                    var pageResponse = await client.GetAsync(nextUrl);
                    pageResponse.EnsureSuccessStatusCode();

                    var pageJson = await pageResponse.Content.ReadAsStringAsync();
                    var trackPage = JsonConvert.DeserializeObject<PlayListTracks.Tracks>(pageJson);

                    if (trackPage?.items != null)
                    {
                        Log.Debug($"Se recuperaron {trackPage.items.Count} canciones");
                        playlistRoot.tracks.items.AddRange(trackPage.items);
                    }
                    else Log.Warn("No se encontraron canciones en esta página");

                    nextUrl = trackPage.next;
                }

                Log.Info($"Se completó la recuperación de canciones. Total: {playlistRoot.tracks.items.Count}");
                return playlistRoot;
            }
            catch (Exception ex)
            {
                Log.Error($"Error al recuperar canciones para la playlist con ID={playlistId}", ex);
                return null;
            }
        }
    }
}