namespace SpotifyDownloader.Helpers
{
    public static class NotifyHelper
    {
        // Instancia única para toda la app
        private static readonly NotifyIcon _notifyIcon = new NotifyIcon
        {
            Visible = false // lo activamos al primer uso
        };

        /// <summary>
        /// Muestra un globo de notificación en la bandeja del sistema.
        /// </summary>
        public static void ShowNotification(string text, string title, int timeout = 5000, string? iconPath = null)
        {
            // Icono obligatorio para que aparezca en la bandeja
            if (_notifyIcon.Icon == null)
            {
                if (!string.IsNullOrEmpty(iconPath))
                    _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                else
                {
                    // Carga un .ico embebido o de disco; **reemplaza** por el tuyo
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }

            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = text;
            _notifyIcon.Text = title; // texto al pasar el ratón
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;

            if (!_notifyIcon.Visible)
                _notifyIcon.Visible = true;  // imprescindible

            _notifyIcon.ShowBalloonTip(timeout);
        }

        /// <summary>
        /// Llama a esto al cerrar la aplicación para limpiar recursos.
        /// </summary>
        public static void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}