namespace SpotifyPlayListDownloader.Services
{
    public class SpotifyService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SpotifyService));

        private readonly string _clientId;
        private readonly string _clientSecret;

        // Cache de token
        private string? _accessToken;

        private DateTimeOffset _tokenFetchedAt = DateTimeOffset.MinValue;
        private static readonly TimeSpan _tokenTtl = TimeSpan.FromMinutes(50);
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        public SpotifyService(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            Log.Info("SpotifyService iniciado");
        }

        /// <summary>
        /// Asegura que hay un token válido en memoria; si no, obtiene uno nuevo.
        /// </summary>
        private async Task<string> EnsureAccessTokenAsync(bool forceRefresh = false)
        {
            // Fast path (sin lock) si tenemos token fresco y no se fuerza refresh
            if (!forceRefresh && _accessToken != null && DateTimeOffset.UtcNow - _tokenFetchedAt < _tokenTtl)
                return _accessToken;

            await _tokenLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // Re-comprobar dentro del lock
                if (!forceRefresh && _accessToken != null && DateTimeOffset.UtcNow - _tokenFetchedAt < _tokenTtl)
                    return _accessToken;

                Log.Info(forceRefresh
                    ? "Forzando refresh del token de acceso (401 detectado)."
                    : "Token ausente o caducado (>50 min). Obteniendo nuevo token...");

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

                var response = await client.SendAsync(request).ConfigureAwait(false);
                Log.Debug($"Respuesta al obtener token de acceso: {response.StatusCode}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                _accessToken = doc.RootElement.GetProperty("access_token").GetString();
                _tokenFetchedAt = DateTimeOffset.UtcNow;

                Log.Info("Token de acceso obtenido/refrescado correctamente");
                return _accessToken!;
            }
            catch (Exception ex)
            {
                Log.Error("Error al obtener el token de acceso", ex);
                throw;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        /// <summary>
        /// Ejecuta una GET a Spotify con el token actual; si devuelve 401, refresca y reintenta una vez.
        /// </summary>
        private async Task<HttpResponseMessage> GetWithAutoRefreshAsync(HttpClient client, string url)
        {
            // 1ª tentativa con token actual/fresco
            var token = await EnsureAccessTokenAsync().ConfigureAwait(false);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await client.GetAsync(url).ConfigureAwait(false);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                resp.Dispose();
                // Forzar refresh y reintentar una sola vez
                var freshToken = await EnsureAccessTokenAsync(forceRefresh: true).ConfigureAwait(false);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", freshToken);

                resp = await client.GetAsync(url).ConfigureAwait(false);
            }
            return resp;
        }

        // ====== API PÚBLICA ======

        // Si aún quieres exponer la obtención del token manualmente, puedes dejar este método,
        // pero ya no hace falta usarlo fuera: las llamadas internas lo gestionan.
        public Task<string> GetAccessTokenAsync() => EnsureAccessTokenAsync();

        public async Task<PlayListTracks.Root?> GetPlaylistTracksAsync(string playlistId)
        {
            Log.Info($"Obteniendo canciones de la playlist con ID={playlistId}");
            try
            {
                var playlistInfoUrl = $"https://api.spotify.com/v1/playlists/{playlistId}";
                var tracksUrl = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?limit=100";

                using var client = new HttpClient();

                // Obtener info básica de la playlist
                Log.Debug($"Obteniendo información de la playlist: {playlistInfoUrl}");
                var response = await GetWithAutoRefreshAsync(client, playlistInfoUrl).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var playlistJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Log.Debug("Información de la playlist obtenida");

                var playlistRoot = JsonConvert.DeserializeObject<PlayListTracks.Root>(playlistJson)
                                   ?? new PlayListTracks.Root();
                playlistRoot.tracks ??= new PlayListTracks.Tracks();
                playlistRoot.tracks.items = new List<PlayListTracks.Item>();

                string? nextUrl = tracksUrl;

                // Obtener todas las páginas de canciones
                while (!string.IsNullOrEmpty(nextUrl))
                {
                    Log.Debug($"Obteniendo canciones de la playlist: {nextUrl}");
                    var pageResponse = await GetWithAutoRefreshAsync(client, nextUrl).ConfigureAwait(false);
                    pageResponse.EnsureSuccessStatusCode();

                    var pageJson = await pageResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var trackPage = JsonConvert.DeserializeObject<PlayListTracks.Tracks>(pageJson);

                    if (trackPage?.items != null)
                    {
                        Log.Debug($"Se recuperaron {trackPage.items.Count} canciones");
                        playlistRoot.tracks.items.AddRange(trackPage.items);
                    }
                    else
                    {
                        Log.Warn("No se encontraron canciones en esta página");
                    }

                    nextUrl = trackPage?.next;
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

        public async Task<ArtistSongs.Root?> GetArtistsTracksAsync(
            string artistId, List<string> llInclude, string market, string limit, string offset)
        {
            Log.Info($"Obteniendo canciones del artista con ID={artistId}");
            try
            {
                var include = Uri.EscapeDataString(string.Join(",", llInclude));
                var url = $"https://api.spotify.com/v1/artists/{artistId}/albums?include_groups={include}&market={market}&limit={limit}&offset={offset}";

                using var client = new HttpClient();

                Log.Debug($"Obteniendo información del artista: {artistId}");

                // Lista acumulada
                var allItems = new List<ArtistSongs.Item>();

                string? nextUrl = url;
                while (!string.IsNullOrEmpty(nextUrl))
                {
                    var response = await GetWithAutoRefreshAsync(client, nextUrl).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var page = JsonConvert.DeserializeObject<ArtistSongs.Root>(json);

                    if (page?.items != null && page.items.Count > 0)
                    {
                        allItems.AddRange(page.items);
                        Log.Debug($"Se recuperaron {page.items.Count} álbumes. Total acumulado: {allItems.Count}");
                    }
                    else
                    {
                        Log.Warn("No se encontraron más álbumes en esta página");
                    }

                    nextUrl = page?.next;
                }

                var finalResult = new ArtistSongs.Root
                {
                    items = allItems
                };

                Log.Info($"Se completó la recuperación de álbumes. Total: {allItems.Count}");
                return finalResult;
            }
            catch (Exception ex)
            {
                Log.Error($"Error al recuperar canciones para el artista con ID={artistId}", ex);
                return null;
            }
        }

        public async Task<AlbumSongs.Root?> GetSongAlumb(string idAlbum, string market = "ES")
        {
            try
            {
                var url = $"https://api.spotify.com/v1/albums/{idAlbum}?market={market}";

                using var client = new HttpClient();

                Log.Debug($"Obteniendo información del album: {idAlbum}");

                var response = await GetWithAutoRefreshAsync(client, url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var album = JsonConvert.DeserializeObject<AlbumSongs.Root>(json);

                return album;
            }
            catch (Exception ex)
            {
                Log.Error($"Error al recuperar canciones para el artista con ID={idAlbum}", ex);
                return null;
            }
        }
    }
}