using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlayListDownloader.Clases
{
    public static class ConfigHelper
    {
        public static AppConfig LoadConfig()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var result = new AppConfig();
            config.Bind(result);
            return result;
        }
    }
}