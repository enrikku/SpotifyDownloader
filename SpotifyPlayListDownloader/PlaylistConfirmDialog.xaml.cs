using System.Windows;
using System.Windows.Media.Imaging;

using log4net;

namespace SpotifyPlayListDownloader
{
    public partial class PlaylistConfirmDialog : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PlaylistConfirmDialog));

        public bool IsConfirmed { get; private set; } = false;

        public PlaylistConfirmDialog(string playlistName, string imageUrl)
        {
            InitializeComponent();

            Log.Info($"Opening PlaylistConfirmDialog for playlist: {playlistName}");

            PlaylistNameText.Text = playlistName;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
                    bitmap.EndInit();
                    PlaylistImage.Source = bitmap;
                    Log.Debug($"Playlist image loaded from URL: {imageUrl}");
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to load playlist image from URL: {imageUrl}. Error: {ex.Message}");
                }
            }
            else Log.Debug("No image URL provided for playlist.");
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("Playlist confirmed by user.");
            IsConfirmed = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("Playlist confirmation cancelled by user.");
            this.Close();
        }
    }
}
