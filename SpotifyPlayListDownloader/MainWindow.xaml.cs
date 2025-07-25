using Microsoft.Win32;
using SpotifyPlayListDownloader.Clases;
using SpotifyPlayListDownloader.Services;
using System.Windows;

namespace SpotifyPlayListDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            string downloadsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            OutputPathTextBox.Text = downloadsPath;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog()
            {
                Title = "Selecciona carpeta de descarga",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                Multiselect = false
            };

            if (dialog.ShowDialog() == true) OutputPathTextBox.Text = dialog.FolderName;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var playlistId = PlaylistIdTextBox.Text;
            var outputPath = OutputPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(playlistId) || string.IsNullOrWhiteSpace(outputPath))
            {
                System.Windows.MessageBox.Show("Por favor, ingrese el ID de la playlist y la ruta de salida.");
                return;
            }

            var config = ConfigHelper.LoadConfig();
            var spotify = new SpotifyService(config.Spotify.ClientId, config.Spotify.ClientSecret);
            var yt = new DownloaderService();

            try
            {
                lblStatus.Content = "Obteniendo canciones...";
                var token = await spotify.GetAccessTokenAsync();
                var playlist = await spotify.GetPlaylistTracksAsync(playlistId, token);

                if (playlist != null)
                {
                    var imageUrl = playlist.images?.FirstOrDefault()?.url;
                    var playlistName = playlist.name;

                    var dialog = new PlaylistConfirmDialog(playlistName, imageUrl);
                    dialog.Owner = this;
                    dialog.ShowDialog();

                    if (!dialog.IsConfirmed)
                    {
                        MessageBox.Show("Descarga cancelada.");
                        return;
                    }

                    lblStatus.Content = "Descargando canciones...";

                    outputPath = System.IO.Path.Combine(OutputPathTextBox.Text, playlist.name);

                    var start = DateTime.Now;
                    var textStatus = "";
                    var qtt = 0;
                    foreach (var item in playlist.tracks.items)
                    {
                        var title = $"{item.track.name} {item.track.artists.FirstOrDefault()?.name}";
                        await yt.DownloadMp3Async(title, outputPath, item);
                        qtt++;
                        lblStatus.Content = $"({qtt}/{playlist.tracks.items.Count}) {title}";
                    }
                    var end = DateTime.Now;
                    var time = end - start;
                    lblStatus.Content = $"Tiempo de descarga: {time.Hours}h {time.Minutes}m {time.Seconds}s";

                    System.Windows.MessageBox.Show("Descarga completa.");
                }
                else System.Windows.MessageBox.Show("Playlist no encontrada.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void ShowHelp_Click(object sender, RoutedEventArgs e)
        {
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