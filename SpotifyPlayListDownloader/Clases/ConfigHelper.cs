namespace SpotifyDownloader.Clases
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