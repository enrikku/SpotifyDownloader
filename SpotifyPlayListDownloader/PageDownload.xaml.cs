namespace SpotifyPlayListDownloader
{
    public partial class PageDownload : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PageDownload));
        private readonly TYPE_DOWNLOAD _type;
        private readonly string _title = "";

        #region "Constructor"

        public PageDownload(TYPE_DOWNLOAD type)
        {
            InitializeComponent();
            _type = type;

            if (_type == TYPE_DOWNLOAD.PLAY_LIST) _title = "Spotify Playlist Downloader";
            else if (_type == TYPE_DOWNLOAD.ARTIST) _title = "Spotify Artist Downloader";
            else _title = "Spotify Downloader";

            this.Title = _title;

            Log.Info("MainWindow iniciado");
            string musicPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            OutputPathTextBox.Text = musicPath;
            Log.Debug($"Ruta de descarga por defecto: {musicPath}");
        }

        #endregion "Constructor"

        #region "Eventos"

        #region "Botones"

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Browse();
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Download();
        }

        private void ShowHelp_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        #endregion "Botones"

        #endregion "Eventos"

        #region Métodos

        private void Download()
        {
            try
            {
                switch (_type)
                {
                    case TYPE_DOWNLOAD.PLAY_LIST:
                        DownloadPlayList();
                        break;

                    case TYPE_DOWNLOAD.ARTIST:
                        DownloadArtist();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error al abrir la ventana de descarga", ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region "Descargar PlayList"

        private async void DownloadPlayList()
        {
            var outputPath = OutputPathTextBox.Text;

            if (!SpotifyHelper.TryGetSpotifyPlaylistId(Url.Text, out var playlistId))
            {
                Log.Warn("Invalid Spotify playlist URL/URI.");
                MessageBox.Show("Por favor, ingrese una URL/URI de una playlist de Spotify válida.");
                return;
            }

            Log.Info($"Botón 'Descargar' pulsado. PlaylistId: {playlistId}, RutaSalida: {outputPath}");

            if (string.IsNullOrWhiteSpace(playlistId) || string.IsNullOrWhiteSpace(outputPath))
            {
                MessageBox.Show("Por favor, ingrese el ID de la playlist y la ruta de salida.");
                Log.Warn("PlaylistId o RutaSalida vacíos. Operación cancelada.");
                return;
            }

            var config = ConfigHelper.LoadConfig();
            var spotify = new SpotifyService(config.Spotify.ClientId, config.Spotify.ClientSecret);
            var yt = new DownloaderService();

            try
            {
                lblStatus.Text = "Obteniendo canciones...";
                Log.Info("Obteniendo canciones de la playlist...");
                var playlist = await spotify.GetPlaylistTracksAsync(playlistId);

                if (playlist != null)
                {
                    var imageUrl = playlist.images?.FirstOrDefault()?.url;
                    var playlistName = playlist.name;
                    Log.Info($"Playlist encontrada: {playlistName}, con {playlist.tracks.items.Count} canciones");

                    var dialog = new PlaylistConfirmDialog(playlistName, imageUrl);
                    dialog.Owner = this;
                    dialog.ShowDialog();

                    if (!dialog.IsConfirmed)
                    {
                        MessageBox.Show("Descarga cancelada.");
                        Log.Info("Descarga cancelada por el usuario");
                        return;
                    }

                    this.Title = $"{_title} - {playlistName}";
                    lblStatus.Text = $"Descargando canciones de la playlist {playlistName}...";
                    outputPath = System.IO.Path.Combine(OutputPathTextBox.Text, playlist.name);
                    Log.Info($"Ruta de salida final: {outputPath}");

                    var start = DateTime.Now;
                    var qtt = 0;

                    foreach (var item in playlist.tracks.items)
                    {
                        if (item.track != null)
                        {
                            var title = $"{item.track.name}";
                            var query = $"{item.track.name} {item.track.artists.FirstOrDefault()?.name}";
                            Log.Debug($"Descargando: {title}");
                            await yt.DownloadMp3Async(query, title, outputPath, item, null);
                            qtt++;
                            lblStatus.Text = $"({qtt}/{playlist.tracks.items.Count}) {title}";
                        }
                        else qtt++;
                    }

                    var end = DateTime.Now;
                    var time = end - start;
                    lblStatus.Text = $"Tiempo de descarga: {time.Hours}h {time.Minutes}m {time.Seconds}s";
                    Log.Info($"Descarga completada. Tiempo total: {time}");

                    var main = Application.Current.MainWindow;
                    if (main != null)
                    {
                        if (main.WindowState == WindowState.Minimized) main.WindowState = WindowState.Normal;

                        main.Activate();
                        main.Topmost = true;
                        main.Topmost = false;

                        MessageBox.Show(main, "Descarga completa.", "Spotify Downloader",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    Log.Warn("Playlist no encontrada");
                    MessageBox.Show("Playlist no encontrada.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error durante la descarga: {ex.Message}", ex);
            }
        }

        #endregion "Descargar PlayList"

        #region "Descargar Artista"

        private async void DownloadArtist()
        {
            try
            {
                if (!SpotifyHelper.TryGetSpotifyArtistId(Url.Text, out var artistId))
                {
                    Log.Warn("Invalid Spotify playlist URL/URI.");
                    MessageBox.Show("Por favor, ingrese una URL/URI de una playlist de Spotify válida.");
                    return;
                }

                var outputPath = OutputPathTextBox.Text;

                Log.Info($"Botón 'Descargar' pulsado. ArtistId: {artistId}, RutaSalida: {outputPath}");

                if (string.IsNullOrWhiteSpace(artistId) || string.IsNullOrWhiteSpace(outputPath))
                {
                    MessageBox.Show("Por favor, ingrese el ID del artista y la ruta de salida.");
                    Log.Warn("ArtistId o RutaSalida vacíos. Operación cancelada.");
                    return;
                }

                var config = ConfigHelper.LoadConfig();
                var spotify = new SpotifyService(config.Spotify.ClientId, config.Spotify.ClientSecret);
                var yt = new DownloaderService();

                lblStatus.Text = "Obteniendo canciones del artista...";
                Log.Info("Obteniendo canciones de la playlist...");

                var albumArtist = await spotify.GetArtistsTracksAsync(artistId, ["album"], "ES", "50", "0");
                var epsArtist = await spotify.GetArtistsTracksAsync(artistId, ["single"], "ES", "50", "0");
                var appearsOnArtist = await spotify.GetArtistsTracksAsync(artistId, ["appears_on"], "ES", "50", "0");

                var artistName = "";

                if (albumArtist != null)
                    if (albumArtist.items.Count > 0)
                        artistName = albumArtist.items[0].artists[0].name;
                    else if (epsArtist != null)
                        if (epsArtist.items.Count > 0)
                            artistName = epsArtist.items[0].artists[0].name;

                outputPath = System.IO.Path.Combine(outputPath, artistName);

                DateTime start = DateTime.Now;
                // Descarga de los albums
                if (albumArtist != null)
                    await DownloadAlbums(albumArtist, outputPath, yt, spotify);

                // Descarga de las EPs
                if (epsArtist != null)
                    await DownloadEPs(epsArtist, outputPath, yt, spotify);

                // Descarga de los EPs
                if (appearsOnArtist != null)
                    await DownloadAppearsOn(appearsOnArtist, outputPath, yt, spotify, artistName);

                var end = DateTime.Now;
                var time = end - start;
                lblStatus.Text = $"Tiempo de descarga: {time.Hours}h {time.Minutes}m {time.Seconds}s";
                Log.Info($"Descarga completada. Tiempo total: {time}");
            }
            catch (Exception ex)
            {
                Log.Error("Error durante la descarga", ex);
            }
        }

        private async Task DownloadAlbums(ArtistSongs.Root albumArtist, string outputPath, DownloaderService yt, SpotifyService spotify)
        {
            if (albumArtist.items.Count > 0)
            {
                var qttAlbums = albumArtist.items.Count;
                var qttAlbumsDownloaded = 0;

                foreach (var item in albumArtist.items)
                {
                    var albumName = PathHelper.SanitizeSimple(item.name);
                    var albumPath = Path.Combine(outputPath, "Albums", albumName);

                    if (!Directory.Exists(albumPath))
                    {
                        Directory.CreateDirectory(albumPath);
                        Log.Info($"Carpeta de album creada: {albumPath}");
                    }

                    // Una vez creada la carpeta hay que buscar las canciones de ese album
                    var albumSongs = await spotify.GetSongAlumb(item.id);

                    if (albumSongs != null)
                    {
                        var qttSongsDownloaded = 0;

                        lblStatus.Text = $"(Albums: 0/{qttAlbums} - Canciones: 0/{albumSongs.tracks.items.Count}";

                        foreach (var track in albumSongs.tracks.items)
                        {
                            if (track != null)
                            {
                                var title = $"{track.name}";
                                var query = $"{track.name} {track.artists.FirstOrDefault()?.name}";

                                Log.Debug($"Descargando: {title}");
                                await yt.DownloadMp3Async(query, title, albumPath, track, item.name, albumSongs);
                                qttSongsDownloaded++;
                                lblStatus.Text = $"(Albums: {qttAlbumsDownloaded}/{qttAlbums} - Canciones: {qttSongsDownloaded}/{albumSongs.tracks.items.Count}) {item.name} - {title}";
                            }
                            else qttSongsDownloaded++;
                        }

                        qttAlbumsDownloaded++;
                    }
                }
            }
        }

        private async Task DownloadEPs(ArtistSongs.Root epsArtist, string outputPath, DownloaderService yt, SpotifyService spotify)
        {
            if (epsArtist.items.Count > 0)
            {
                var qttSingles = epsArtist.items.Count;
                var qttSinglesDownloaded = 0;

                var nameArtist = epsArtist.items[0].artists[0].name;
                outputPath = System.IO.Path.Combine(outputPath, "EPS");

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                    Log.Info($"Carpeta de EPS creada: {outputPath}");
                }

                foreach (var item in epsArtist.items)
                {
                    var epArist = await spotify.GetSongAlumb(item.id);

                    if (epArist != null)
                    {
                        lblStatus.Text = $"(EP: {qttSinglesDownloaded}/{qttSingles} - Canciones: 0/{epArist.tracks.items.Count})";
                        var qttSongsDownloaded = 0;

                        foreach (var track in epArist.tracks.items)
                        {
                            if (track != null)
                            {
                                var title = $"{track.name}";
                                var query = $"{title} {item.artists.FirstOrDefault()?.name}";
                                Log.Debug($"Descargando: {title}");
                                await yt.DownloadMp3Async(query, title, outputPath, track, item.name, epArist);
                                qttSongsDownloaded++;
                                lblStatus.Text = $"(EP: {qttSinglesDownloaded}/{qttSingles} - Canciones: {qttSongsDownloaded}/{epArist.tracks.items.Count}) {item.name} - {title}";
                            }
                            else qttSongsDownloaded++;
                        }

                        qttSinglesDownloaded++;
                    }
                }
            }
        }

        private async Task DownloadAppearsOn(ArtistSongs.Root appearsOnArtist, string outputPath, DownloaderService yt, SpotifyService spotify, string artistName)
        {
            if (appearsOnArtist.items.Count > 0)
            {
                var qttAppears = 0;
                var qttAppearsDownloaded = 0;

                outputPath = System.IO.Path.Combine(outputPath, "AparecenEn");

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                    Log.Info($"Carpeta de aparecen en creada: {outputPath}");
                }

                var qttSongs = 0;
                var qttSongsDownloaded = 0;
                var songs = new List<AlbumSongs.Item>();

                foreach (var item in appearsOnArtist.items)
                {
                    var epArist = await spotify.GetSongAlumb(item.id);

                    if (epArist != null)
                    {
                        var songsArtist = epArist.tracks.items.Where(x => x.artists.Any(y => y.name == artistName)).ToList();
                        songs.AddRange(songsArtist);
                    }
                }

                qttSongs = songs.Count();
                lblStatus.Text = $"Canciones: 0/{qttSongs})";

                foreach (var item in songs)
                {
                    if (item != null)
                    {
                        var title = $"{item.name}";
                        var query = $"{title} {item.artists.FirstOrDefault()?.name}";
                        Log.Debug($"Descargando: {title}");
                        await yt.DownloadMp3Async(query, title, outputPath, item, item.name);
                        qttSongsDownloaded++;
                        lblStatus.Text = $"Canciones: {qttSongsDownloaded}/{qttSongs}) {item.name} - {title}";
                    }
                    else qttSongsDownloaded++;
                }
            }
        }

        #endregion "Descargar Artista"

        private void Browse()
        {
            try
            {
                Log.Info("Botón 'Examinar' pulsado: seleccionando carpeta de descarga");
                var dialog = new OpenFolderDialog()
                {
                    Title = "Selecciona carpeta de descarga",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    OutputPathTextBox.Text = dialog.FolderName;
                    Log.Info($"Carpeta de descarga seleccionada: {dialog.FolderName}");
                }
                else Log.Debug("Selección de carpeta cancelada");
            }
            catch (Exception ex)
            {
                Log.Error("Error al abrir la ventana de descarga", ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowHelp()
        {
            try
            {
                Log.Info("Mostrando ayuda para ingresar la URL de la playlist");

                if (_type == TYPE_DOWNLOAD.PLAY_LIST)
                {
                    MessageBox.Show(
                        "Cómo usar la aplicación:\n\n" +
                        "1. Abre Spotify y ve a la playlist que deseas descargar.\n" +
                        "2. Haz clic en los tres puntos (...) > Compartir > Copiar enlace de la playlist.\n" +
                        "3. Pega la URL completa en la aplicación.\n\n" +
                        "Ejemplo:\nhttps://open.spotify.com/playlist/7f9gfBelzjvoaKu8HbNmox\n\n" +
                        "¡Eso es todo! La aplicación extraerá el ID automáticamente.",
                        "Cómo usar la URL de la playlist",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else if (_type == TYPE_DOWNLOAD.ARTIST)
                {
                    MessageBox.Show(
                        "Cómo usar la aplicación:\n\n" +
                        "1. Abre Spotify y ve al artista que deseas descargar.\n" +
                        "2. Haz clic en los tres puntos (...) > Compartir > Copiar enlace del artista.\n" +
                        "3. Pega la URL completa en la aplicación.\n\n" +
                        "Ejemplo:\nhttps://open.spotify.com/intl-es/artist/1DH9RJ0xBVje6gQmK3LWUY?si=3ifFuziOTBK7qLpeasHeKA\n\n" +
                        "¡Eso es todo! La aplicación extraerá el ID automáticamente.",
                        "Cómo usar la URL del artista",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error al mostrar ayuda", ex);
            }
        }

        #endregion Métodos
    }
}