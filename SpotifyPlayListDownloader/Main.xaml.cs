using MessageBox = System.Windows.MessageBox;

namespace SpotifyDownloader
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

        private void btnDeleteCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = Path.Combine(
                                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                                "SpotifyDownloader", "image_cache");

                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.GetFiles(path))
                        File.Delete(file);

                    foreach (var dir in Directory.GetDirectories(path))
                        Directory.Delete(dir, true);

                    MessageBox.Show("Contenido de la carpeta eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                    MessageBox.Show("La carpeta no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
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