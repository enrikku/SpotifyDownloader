using System.Windows;
using System.Windows.Media.Imaging;

namespace SpotifyPlayListDownloader
{
    public partial class PlaylistConfirmDialog : Window
    {
        public bool IsConfirmed { get; private set; } = false;

        public PlaylistConfirmDialog(string playlistName, string imageUrl)
        {
            InitializeComponent();

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
                }
                catch
                {
                    // ignore image load errors
                }
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}