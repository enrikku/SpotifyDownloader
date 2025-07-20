using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlayListDownloader.Clases
{
    public class SpotifySettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class AppConfig
    {
        public SpotifySettings Spotify { get; set; }
    }
}