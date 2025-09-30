namespace SpotifyDownloader
{
    public partial class PlaylistConfirmDialog : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PlaylistConfirmDialog));

        public bool IsConfirmed { get; private set; } = false;

        public PlaylistConfirmDialog(string playlistName, string? imageUrl)
        {
            InitializeComponent();

            Log.Info($"Abriendo PlaylistConfirmDialog para la playlist: {playlistName}");

            PlaylistNameText.Text = playlistName;

            try
            {
                if (imageUrl != null)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
                    bitmap.EndInit();
                    PlaylistImage.Source = bitmap;
                }
                else Log.Warn("La URL de la imagen de la playlist es nula.");

                Log.Debug($"Imagen de la playlist cargada desde la URL: {imageUrl}");
            }
            catch (Exception ex)
            {
                Log.Warn($"Error al cargar la imagen de la playlist desde la URL: {imageUrl}. Detalle: {ex.Message}");
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("Playlist confirmada por el usuario.");
            IsConfirmed = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("Confirmación de la playlist cancelada por el usuario.");
            this.Close();
        }
    }
}