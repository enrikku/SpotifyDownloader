namespace SpotifyPlayListDownloader
{
    /// <summary>
    /// Lógica de interacción para Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Main));

        public Main()
        {
            InitializeComponent();
            Log.Info("Main iniciado");
        }

        private void btnDownloadPlayList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Info("Botón 'Descargar playlist' pulsado");
                var win = new PageDownload(TYPE_DOWNLOAD.PLAY_LIST);
                win.Show();
                //this.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Error al abrir la ventana de descarga de playlist", ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDownloadArtist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Info("Botón 'Descargar artista' pulsado");
                var win = new PageDownload(TYPE_DOWNLOAD.ARTIST);
                win.Show();
            }
            catch (Exception ex)
            {
                Log.Error("Error al abrir la ventana de descarga de artista", ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}