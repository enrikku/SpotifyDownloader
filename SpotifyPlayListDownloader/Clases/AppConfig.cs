namespace SpotifyDownloader.Clases
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