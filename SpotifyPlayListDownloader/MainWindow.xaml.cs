using Microsoft.Win32;
using SpotifyPlayListDownloader.Clases;
using SpotifyPlayListDownloader.Services;
using System.Windows;
using log4net;

namespace SpotifyPlayListDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();
            Log.Info("MainWindow initialized");

            string downloadsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            OutputPathTextBox.Text = downloadsPath;
            Log.Debug($"Default download path set to: {downloadsPath}");
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("BrowseButton clicked: selecting download folder");
            var dialog = new OpenFolderDialog()
            {
                Title = "Selecciona carpeta de descarga",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                OutputPathTextBox.Text = dialog.FolderName;
                Log.Info($"Download folder selected: {dialog.FolderName}");
            }
            else
            {
                Log.Debug("Folder selection cancelled");
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var playlistId = PlaylistIdTextBox.Text;
            var outputPath = OutputPathTextBox.Text;
            Log.Info($"DownloadButton clicked. PlaylistId: {playlistId}, OutputPath: {outputPath}");

            if (string.IsNullOrWhiteSpace(playlistId) || string.IsNullOrWhiteSpace(outputPath))
            {
                MessageBox.Show("Por favor, ingrese el ID de la playlist y la ruta de salida.");
                Log.Warn("PlaylistId o OutputPath vacíos. Operación cancelada.");
                return;
            }

            var config = ConfigHelper.LoadConfig();
            var spotify = new SpotifyService(config.Spotify.ClientId, config.Spotify.ClientSecret);
            var yt = new DownloaderService();

            try
            {
                lblStatus.Content = "Obteniendo canciones...";
                Log.Info("Obteniendo access token...");
                var token = await spotify.GetAccessTokenAsync();

                Log.Info("Obteniendo playlist tracks...");
                var playlist = await spotify.GetPlaylistTracksAsync(playlistId, token);

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

                    lblStatus.Content = "Descargando canciones...";
                    outputPath = System.IO.Path.Combine(OutputPathTextBox.Text, playlist.name);
                    Log.Info($"Ruta de salida final: {outputPath}");

                    var start = DateTime.Now;
                    var qtt = 0;
                    foreach (var item in playlist.tracks.items)
                    {
                        var title = $"{item.track.name} {item.track.artists.FirstOrDefault()?.name}";
                        Log.Debug($"Descargando: {title}");
                        await yt.DownloadMp3Async(title, outputPath, item);
                        qtt++;
                        lblStatus.Content = $"({qtt}/{playlist.tracks.items.Count}) {title}";
                    }
                    var end = DateTime.Now;
                    var time = end - start;
                    lblStatus.Content = $"Tiempo de descarga: {time.Hours}h {time.Minutes}m {time.Seconds}s";
                    Log.Info($"Descarga completada. Tiempo total: {time}");

                    MessageBox.Show("Descarga completa.");
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
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void ShowHelp_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("Mostrando ayuda para obtener el ID de la playlist");
            MessageBox.Show(
                "Cómo obtener el ID de la playlist:\n\n" +
                "1. Abre Spotify y ve a la playlist que deseas descargar.\n" +
                "2. Haz clic en los tres puntos (...) > Compartir > Copiar enlace de la playlist.\n" +
                "3. Pega el enlace aquí, o copia solo la parte después de '/playlist/'.\n\n" +
                "Ejemplo:\nhttps://open.spotify.com/playlist/37i9dQZF1DXcBWIGoYBM5M\n\n" +
                "El ID de la playlist es: \"37i9dQZF1DXcBWIGoYBM5M\"",
                "¿Cómo obtener el ID de la playlist?",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
