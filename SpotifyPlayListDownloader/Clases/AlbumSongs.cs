namespace SpotifyPlayListDownloader.Clases
{
    public class AlbumSongs
    {
        public class Artist
        {
            public ExternalUrls external_urls { get; set; }
            public string href { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string uri { get; set; }
        }

        public class Copyright
        {
            public string text { get; set; }
            public string type { get; set; }
        }

        public class ExternalIds
        {
            public string upc { get; set; }
        }

        public class ExternalUrls
        {
            public string spotify { get; set; }
        }

        public class Image
        {
            public string url { get; set; }
            public int height { get; set; }
            public int width { get; set; }
        }

        public class Item
        {
            public List<Artist> artists { get; set; }
            public List<string> available_markets { get; set; }
            public int disc_number { get; set; }
            public int duration_ms { get; set; }
            public bool @explicit { get; set; }
            public ExternalUrls external_urls { get; set; }
            public string href { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public object preview_url { get; set; }
            public int track_number { get; set; }
            public string type { get; set; }
            public string uri { get; set; }
            public bool is_local { get; set; }
        }

        public class Root
        {
            public string album_type { get; set; }
            public int total_tracks { get; set; }
            public List<string> available_markets { get; set; }
            public ExternalUrls external_urls { get; set; }
            public string href { get; set; }
            public string id { get; set; }
            public List<Image> images { get; set; }
            public string name { get; set; }
            public string release_date { get; set; }
            public string release_date_precision { get; set; }
            public string type { get; set; }
            public string uri { get; set; }
            public List<Artist> artists { get; set; }
            public Tracks tracks { get; set; }
            public List<Copyright> copyrights { get; set; }
            public ExternalIds external_ids { get; set; }
            public List<object> genres { get; set; }
            public string label { get; set; }
            public int popularity { get; set; }
        }

        public class Tracks
        {
            public string href { get; set; }
            public int limit { get; set; }
            public object next { get; set; }
            public int offset { get; set; }
            public object previous { get; set; }
            public int total { get; set; }
            public List<Item> items { get; set; }
        }
    }
}