using MessageBox = System.Windows.MessageBox;

namespace SpotifyPlayListDownloader
{
    public partial class Main : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Main));

        #region Constructor

        public Main()
        {
            InitializeComponent();
            Log.Info("Main iniciado");
        }

        #endregion Constructor

        #region Eventos

        #region Botones

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

        private void btnOpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = log4net.GlobalContext.Properties["LogDir"];

                if (path == null) return;
                if (!Directory.Exists(path.ToString())) return;

                Process.Start("explorer.exe", "/select,\"" + path + "\"");
            }
            catch (Exception ex)
            {
                Log.Error("Error al abrir la ventana de descarga de playlist", ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion Botones

        #endregion Eventos
    }
}