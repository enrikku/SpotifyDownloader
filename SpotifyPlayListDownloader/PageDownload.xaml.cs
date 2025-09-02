using static SpotifyPlayListDownloader.Clases.PlayListTracks;

namespace SpotifyPlayListDownloader
{
    public partial class PageDownload : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PageDownload));
        private readonly TYPE_DOWNLOAD _type;
        private readonly string _title = "";

        private List<TrackInfo> llTracks = new List<TrackInfo>();

        #region "Constructor"

        public PageDownload(TYPE_DOWNLOAD type)
        {
            InitializeComponent();
            _type = type;

            if (_type == TYPE_DOWNLOAD.PLAY_LIST)
            {
                _title = "Spotify Playlist Downloader";
                ImportButton.Visibility = Visibility.Hidden;
            }
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

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();

            if (_type == TYPE_DOWNLOAD.PLAY_LIST)
            {
                ofd = new OpenFileDialog
                {
                    Title = "Selecciona el fichero .txt con URLs de playlists",
                    Filter = "Texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                    Multiselect = false,
                    CheckFileExists = true,
                    CheckPathExists = true
                };
            }
            else if (_type == TYPE_DOWNLOAD.ARTIST)
            {
                ofd = new OpenFileDialog
                {
                    Title = "Selecciona el fichero .txt con URLs de artistas",
                    Filter = "Texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                    Multiselect = false,
                    CheckFileExists = true,
                    CheckPathExists = true
                };
            }

            if (ofd.ShowDialog() != true) return;

            try
            {
                var allText = File.ReadAllText(ofd.FileName);
                var lines = allText
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                if (lines.Count == 0)
                {
                    MessageBox.Show("El fichero está vacío.", "Importar", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool allLinesAreValid = true;

                foreach (var line in lines)
                {
                    if (_type == TYPE_DOWNLOAD.PLAY_LIST)
                    {
                        if (!SpotifyHelper.TryGetSpotifyArtistId(line, out var artistId))
                        {
                            Log.Error($"La URL/URI '{line}' no es una URL de una playlist de Spotify válida.");
                            MessageBox.Show($"La URL/URI '{line}' no es una URL de una playlist de Spotify válida.", "Importar", MessageBoxButton.OK, MessageBoxImage.Warning);
                            allLinesAreValid = false;
                            break;
                        }
                    }
                    else if (_type == TYPE_DOWNLOAD.ARTIST)
                    {
                        if (!SpotifyHelper.TryGetSpotifyArtistId(line, out var artistId))
                        {
                            Log.Error($"La URL/URI '{line}' no es una URL de un artista de Spotify válida.");
                            MessageBox.Show($"La URL/URI '{line}' no es una URL de un artista de Spotify válida.", "Importar", MessageBoxButton.OK, MessageBoxImage.Warning);
                            allLinesAreValid = false;
                            break;
                        }
                    }
                }

                if (allLinesAreValid)
                {
                    llTracks.Clear();

                    var start = DateTime.Now;
                    foreach (var line in lines)
                    {
                        Url.Text = line;
                        await Download(true);
                    }

                    var downloadService = new DownloaderService();

                    var semaphore = new SemaphoreSlim(5);
                    int total = llTracks.Count;
                    int completed = 0;

                    lblStatus.Text = $"Completadas: 0/{total}";

                    var tasks = new List<Task>();

                    foreach (var track in llTracks)
                    {
                        await semaphore.WaitAsync();
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                downloadService.DownloadMp3(track);
                            }
                            finally
                            {
                                int done = Interlocked.Increment(ref completed);
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    lblStatus.Text = $"{done}/{total}";
                                });
                                semaphore.Release();
                            }
                        }));
                    }

                    await Task.WhenAll(tasks);

                    lblStatus.Text = $"Completadas: {completed}/{total}";

                    var end = DateTime.Now;
                    var elapsed = end - start;

                    string elapsedFormatted = $"{(int)elapsed.TotalHours:00}h {elapsed.Minutes:00}m {elapsed.Seconds:00}s";

                    Log.Info($"Importado {lines.Count} {_type}s en {elapsedFormatted}.");
                    MessageBox.Show($"Importado {lines.Count} {_type}s en {elapsedFormatted}.", "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error al leer el fichero: {ex.Message}", ex);
                MessageBox.Show($"Error al leer el fichero: {ex.Message}", "Importar", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion "Botones"

        #endregion "Eventos"

        #region Métodos

        private async Task Download(bool isImport = false)
        {
            try
            {
                switch (_type)
                {
                    case TYPE_DOWNLOAD.PLAY_LIST:
                        await DownloadPlayList(isImport);
                        break;

                    case TYPE_DOWNLOAD.ARTIST:
                        await DownloadArtist(isImport);
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

        private async Task DownloadPlayList(bool isImport = false)
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

                    var llTracksPlayList = new List<TrackInfo>();

                    foreach (var track in playlist.tracks.items)
                    {
                        var loTrackInfo = new TrackInfo();

                        loTrackInfo.album = track.track.album.name;
                        loTrackInfo.name = track.track.name;
                        foreach (var artist in track.track.artists) loTrackInfo.artists.Add(artist.name);

                        uint year = 0;
                        if (!string.IsNullOrEmpty(track.track.album.release_date) && track.track.album.release_date.Length != 4)
                            year = uint.Parse(track.track.album.release_date.Substring(0, 4));

                        loTrackInfo.year = year;
                        loTrackInfo.path = Path.Combine(outputPath, playlistName, $"{loTrackInfo.name}.mp3");
                        llTracksPlayList.Add(loTrackInfo);
                    }

                    var start = DateTime.Now;
                    var downloadService = new DownloaderService();
                    var semaphore = new SemaphoreSlim(5);
                    int total = llTracksPlayList.Count;
                    int completed = 0;

                    lblStatus.Text = $"Completadas: 0/{total}";

                    var tasks = new List<Task>();

                    foreach (var track in llTracksPlayList)
                    {
                        await semaphore.WaitAsync();
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                downloadService.DownloadMp3(track);
                            }
                            finally
                            {
                                int done = Interlocked.Increment(ref completed);
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    lblStatus.Text = $"{done}/{total}";
                                });
                                semaphore.Release();
                            }
                        }));
                    }

                    await Task.WhenAll(tasks);

                    lblStatus.Text = $"Completadas: {completed}/{total}";

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

        private async Task DownloadArtist(bool isImport = false)
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

                artistName = PathHelper.SanitizeSimple(artistName);
                lblStatus.Text = $"Obteniendo canciones de {artistName}...";

                if (isImport)
                {
                    if (albumArtist != null)
                    {
                        int totalAlbums = albumArtist.items.Count;
                        int albumIndex = 1;

                        foreach (var album in albumArtist.items)
                        {
                            string albumName = PathHelper.SanitizeSimple(album.name);
                            lblStatus.Text = $"Procesando álbum {albumIndex}/{totalAlbums}: {albumName} de {artistName}...";

                            if (chkSingleFolder.IsChecked == false)
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName, "Albums", albumName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName, "Albums", albumName));
                            }
                            else
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName));
                            }

                            await Task.Delay(50);

                            uint year = 0;
                            if (!string.IsNullOrEmpty(album.release_date) && album.release_date.Length != 4)
                                year = uint.Parse(album.release_date.Substring(0, 4));

                            // Obtener canciones del álbum
                            var albumSongs = await spotify.GetSongAlumb(album.id);

                            if (albumSongs != null)
                            {
                                int totalTracks = albumSongs.tracks.items.Count;
                                int trackIndex = 1;

                                foreach (var track in albumSongs.tracks.items)
                                {
                                    var newTracks = new TrackInfo();
                                    var trackName = PathHelper.SanitizeSimple(track.name);

                                    newTracks.name = trackName;
                                    foreach (var artist in track.artists)
                                        newTracks.artists.Add(artist.name);

                                    newTracks.album = albumName;
                                    newTracks.year = year;
                                    newTracks.isDownloaded = false;

                                    if (chkSingleFolder.IsChecked == false)
                                        newTracks.path = Path.Combine(outputPath, artistName, "Albums", albumName, trackName + ".mp3");
                                    else
                                        newTracks.path = Path.Combine(outputPath, artistName, trackName + ".mp3");

                                    llTracks.Add(newTracks);

                                    lblStatus.Text = $"Álbum {albumIndex}/{totalAlbums}: {albumName} → {trackIndex}/{totalTracks} canciones procesadas";
                                    trackIndex++;
                                }
                            }

                            albumIndex++;
                        }
                    }

                    if (epsArtist != null)
                    {
                        int totalAlbums = epsArtist.items.Count;
                        int albumIndex = 1;

                        foreach (var album in epsArtist.items)
                        {
                            string albumName = PathHelper.SanitizeSimple(album.name);
                            lblStatus.Text = $"Procesando álbum {albumIndex}/{totalAlbums}: {albumName} de {artistName}...";

                            if (chkSingleFolder.IsChecked == false)
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName, "EPS", albumName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName, "EPS", albumName));
                            }
                            else
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName));
                            }

                            await Task.Delay(50);

                            uint year = 0;
                            if (!string.IsNullOrEmpty(album.release_date) && album.release_date.Length != 4)
                                year = uint.Parse(album.release_date.Substring(0, 4));

                            // Obtener canciones del álbum
                            var albumSongs = await spotify.GetSongAlumb(album.id);

                            if (albumSongs != null)
                            {
                                int totalTracks = albumSongs.tracks.items.Count;
                                int trackIndex = 1;

                                foreach (var track in albumSongs.tracks.items)
                                {
                                    var newTracks = new TrackInfo();
                                    var trackName = PathHelper.SanitizeSimple(track.name);

                                    newTracks.name = trackName;
                                    foreach (var artist in track.artists)
                                        newTracks.artists.Add(artist.name);

                                    newTracks.album = albumName;
                                    newTracks.year = year;
                                    newTracks.isDownloaded = false;

                                    if (chkSingleFolder.IsChecked == false)
                                        newTracks.path = Path.Combine(outputPath, artistName, "EPS", albumName, trackName + ".mp3");
                                    else
                                        newTracks.path = Path.Combine(outputPath, artistName, trackName + ".mp3");

                                    llTracks.Add(newTracks);

                                    lblStatus.Text = $"Álbum {albumIndex}/{totalAlbums}: {albumName} → {trackIndex}/{totalTracks} canciones procesadas";
                                    trackIndex++;
                                }
                            }

                            albumIndex++;
                        }
                    }

                    if (appearsOnArtist != null)
                    {
                        int totalAppears = appearsOnArtist.items.Count;
                        int appearIndex = 1;

                        foreach (var appear in appearsOnArtist.items)
                        {
                            string appearName = PathHelper.SanitizeSimple(appear.name);
                            lblStatus.Text = $"Procesando aparicion {appearIndex}/{totalAppears}: {appearName} de {artistName}...";

                            if (chkSingleFolder.IsChecked == false)
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName, "Appears", appearName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName, "Appears", appearName));
                            }
                            else
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName));
                            }

                            await Task.Delay(50);

                            uint year = 0;
                            if (!string.IsNullOrEmpty(appear.release_date) && appear.release_date.Length != 4)
                                year = uint.Parse(appear.release_date.Substring(0, 4));

                            // Obtener canciones del álbum
                            var appearSongs = await spotify.GetSongAlumb(appear.id);

                            if (appearSongs != null)
                            {
                                appearSongs.tracks.items = appearSongs.tracks.items.Where(x => x.artists.Any(y => y.name == artistName)).ToList();

                                int totalTracks = appearSongs.tracks.items.Count;
                                int trackIndex = 1;

                                foreach (var track in appearSongs.tracks.items)
                                {
                                    var newTracks = new TrackInfo();
                                    var trackName = PathHelper.SanitizeSimple(track.name);

                                    newTracks.name = trackName;
                                    foreach (var artist in track.artists)
                                        newTracks.artists.Add(artist.name);

                                    newTracks.album = appearName;
                                    newTracks.year = year;
                                    newTracks.isDownloaded = false;

                                    if (chkSingleFolder.IsChecked == false)
                                        newTracks.path = Path.Combine(outputPath, artistName, "Appears", appearName, trackName + ".mp3");
                                    else
                                        newTracks.path = Path.Combine(outputPath, artistName, trackName + ".mp3");

                                    llTracks.Add(newTracks);

                                    lblStatus.Text = $"Aparicion {appearIndex}/{totalAppears}: {appearName} → {trackIndex}/{totalTracks} canciones procesadas";
                                    trackIndex++;
                                }
                            }

                            appearIndex++;
                        }
                    }

                    lblStatus.Text = "Todos los álbumes y canciones han sido procesados correctamente.";
                }
                else
                {
                    if (albumArtist != null)
                    {
                        int totalAlbums = albumArtist.items.Count;
                        int albumIndex = 1;

                        foreach (var album in albumArtist.items)
                        {
                            string albumName = PathHelper.SanitizeSimple(album.name);
                            lblStatus.Text = $"Procesando álbum {albumIndex}/{totalAlbums}: {albumName} de {artistName}...";

                            if (chkSingleFolder.IsChecked == false)
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName, "Albums", albumName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName, "Albums", albumName));
                            }
                            else
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName));
                            }

                            await Task.Delay(50);

                            uint year = 0;
                            if (!string.IsNullOrEmpty(album.release_date) && album.release_date.Length != 4)
                                year = uint.Parse(album.release_date.Substring(0, 4));

                            // Obtener canciones del álbum
                            var albumSongs = await spotify.GetSongAlumb(album.id);

                            if (albumSongs != null)
                            {
                                int totalTracks = albumSongs.tracks.items.Count;
                                int trackIndex = 1;

                                foreach (var track in albumSongs.tracks.items)
                                {
                                    var newTracks = new TrackInfo();
                                    var trackName = PathHelper.SanitizeSimple(track.name);

                                    newTracks.name = trackName;
                                    foreach (var artist in track.artists)
                                        newTracks.artists.Add(artist.name);

                                    newTracks.album = albumName;
                                    newTracks.year = year;
                                    newTracks.isDownloaded = false;

                                    if (chkSingleFolder.IsChecked == false)
                                        newTracks.path = Path.Combine(outputPath, artistName, "Albums", albumName, trackName + ".mp3");
                                    else
                                        newTracks.path = Path.Combine(outputPath, artistName, trackName + ".mp3");

                                    llTracks.Add(newTracks);

                                    lblStatus.Text = $"Álbum {albumIndex}/{totalAlbums}: {albumName} → {trackIndex}/{totalTracks} canciones procesadas";
                                    trackIndex++;
                                }
                            }

                            albumIndex++;
                        }
                    }

                    if (epsArtist != null)
                    {
                        int totalAlbums = epsArtist.items.Count;
                        int albumIndex = 1;

                        foreach (var album in epsArtist.items)
                        {
                            string albumName = PathHelper.SanitizeSimple(album.name);
                            lblStatus.Text = $"Procesando álbum {albumIndex}/{totalAlbums}: {albumName} de {artistName}...";

                            if (chkSingleFolder.IsChecked == false)
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName, "EPS", albumName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName, "EPS", albumName));
                            }
                            else
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName));
                            }

                            await Task.Delay(50);

                            uint year = 0;
                            if (!string.IsNullOrEmpty(album.release_date) && album.release_date.Length != 4)
                                year = uint.Parse(album.release_date.Substring(0, 4));

                            // Obtener canciones del álbum
                            var albumSongs = await spotify.GetSongAlumb(album.id);

                            if (albumSongs != null)
                            {
                                int totalTracks = albumSongs.tracks.items.Count;
                                int trackIndex = 1;

                                foreach (var track in albumSongs.tracks.items)
                                {
                                    var newTracks = new TrackInfo();
                                    var trackName = PathHelper.SanitizeSimple(track.name);

                                    newTracks.name = trackName;
                                    foreach (var artist in track.artists)
                                        newTracks.artists.Add(artist.name);

                                    newTracks.album = albumName;
                                    newTracks.year = year;
                                    newTracks.isDownloaded = false;

                                    if (chkSingleFolder.IsChecked == false)
                                        newTracks.path = Path.Combine(outputPath, artistName, "EPS", albumName, trackName + ".mp3");
                                    else
                                        newTracks.path = Path.Combine(outputPath, artistName, trackName + ".mp3");

                                    llTracks.Add(newTracks);

                                    lblStatus.Text = $"Álbum {albumIndex}/{totalAlbums}: {albumName} → {trackIndex}/{totalTracks} canciones procesadas";
                                    trackIndex++;
                                }
                            }

                            albumIndex++;
                        }
                    }

                    if (appearsOnArtist != null)
                    {
                        int totalAppears = appearsOnArtist.items.Count;
                        int appearIndex = 1;

                        foreach (var appear in appearsOnArtist.items)
                        {
                            string appearName = PathHelper.SanitizeSimple(appear.name);
                            lblStatus.Text = $"Procesando aparicion {appearIndex}/{totalAppears}: {appearName} de {artistName}...";

                            if (chkSingleFolder.IsChecked == false)
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName, "Appears", appearName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName, "Appears", appearName));
                            }
                            else
                            {
                                if (!Directory.Exists(Path.Combine(outputPath, artistName)))
                                    Directory.CreateDirectory(Path.Combine(outputPath, artistName));
                            }

                            await Task.Delay(50);

                            uint year = 0;
                            if (!string.IsNullOrEmpty(appear.release_date) && appear.release_date.Length != 4)
                                year = uint.Parse(appear.release_date.Substring(0, 4));

                            // Obtener canciones del álbum
                            var appearSongs = await spotify.GetSongAlumb(appear.id);

                            if (appearSongs != null)
                            {
                                appearSongs.tracks.items = appearSongs.tracks.items.Where(x => x.artists.Any(y => y.name == artistName)).ToList();

                                int totalTracks = appearSongs.tracks.items.Count;
                                int trackIndex = 1;

                                foreach (var track in appearSongs.tracks.items)
                                {
                                    var newTracks = new TrackInfo();
                                    var trackName = PathHelper.SanitizeSimple(track.name);

                                    newTracks.name = trackName;
                                    foreach (var artist in track.artists)
                                        newTracks.artists.Add(artist.name);

                                    newTracks.album = appearName;
                                    newTracks.year = year;
                                    newTracks.isDownloaded = false;

                                    if (chkSingleFolder.IsChecked == false)
                                        newTracks.path = Path.Combine(outputPath, artistName, "Appears", appearName, trackName + ".mp3");
                                    else
                                        newTracks.path = Path.Combine(outputPath, artistName, trackName + ".mp3");

                                    llTracks.Add(newTracks);

                                    lblStatus.Text = $"Aparicion {appearIndex}/{totalAppears}: {appearName} → {trackIndex}/{totalTracks} canciones procesadas";
                                    trackIndex++;
                                }
                            }

                            appearIndex++;
                        }
                    }

                    lblStatus.Text = "Todos los álbumes y canciones han sido procesados correctamente.";

                    var start = DateTime.Now;
                    var downloadService = new DownloaderService();
                    var semaphore = new SemaphoreSlim(5);
                    int total = llTracks.Count;
                    int completed = 0;

                    lblStatus.Text = $"Completadas: 0/{total}";

                    var tasks = new List<Task>();

                    foreach (var track in llTracks)
                    {
                        await semaphore.WaitAsync();
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                downloadService.DownloadMp3(track);
                            }
                            finally
                            {
                                int done = Interlocked.Increment(ref completed);
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    lblStatus.Text = $"{done}/{total}";
                                });
                                semaphore.Release();
                            }
                        }));
                    }

                    await Task.WhenAll(tasks);

                    lblStatus.Text = $"Completadas: {completed}/{total}";

                    var end = DateTime.Now;
                    var time = end - start;
                    lblStatus.Text = $"Tiempo de descarga: {time.Hours}h {time.Minutes}m {time.Seconds}s";
                    Log.Info($"Descarga completada. Tiempo total: {time}");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error durante la descarga", ex);
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