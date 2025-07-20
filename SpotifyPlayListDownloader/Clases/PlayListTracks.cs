namespace SpotifyPlayListDownloader.Clases
{
    public class PlayListTracks
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class AddedBy
        {
            public ExternalUrls external_urls { get; set; }
            public string href { get; set; }
            public string id { get; set; }
            public string type { get; set; }
            public string uri { get; set; }
        }

        public class Album
        {
            public List<string> available_markets { get; set; }
            public string type { get; set; }
            public string album_type { get; set; }
            public string href { get; set; }
            public string id { get; set; }
            public List<Image> images { get; set; }
            public string name { get; set; }
            public string release_date { get; set; }
            public string release_date_precision { get; set; }
            public string uri { get; set; }
            public List<Artist> artists { get; set; }
            public ExternalUrls external_urls { get; set; }
            public int total_tracks { get; set; }
        }

        public class Artist
        {
            public ExternalUrls external_urls { get; set; }
            public string href { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string uri { get; set; }
        }

        public class ExternalIds
        {
            public string isrc { get; set; }
        }

        public class ExternalUrls
        {
            public string spotify { get; set; }
        }

        public class Followers
        {
            public object href { get; set; }
            public int total { get; set; }
        }

        public class Image
        {
            public object height { get; set; }
            public string url { get; set; }
            public object width { get; set; }
        }

        public class Item
        {
            public DateTime added_at { get; set; }
            public AddedBy added_by { get; set; }
            public bool is_local { get; set; }
            public object primary_color { get; set; }
            public Track track { get; set; }
            public VideoThumbnail video_thumbnail { get; set; }
        }

        public class Owner
        {
            public string display_name { get; set; }
            public ExternalUrls external_urls { get; set; }
            public string href { get; set; }
            public string id { get; set; }
            public string type { get; set; }
            public string uri { get; set; }
        }

        public class Root
        {
            public bool collaborative { get; set; }
            public string description { get; set; }
            public ExternalUrls external_urls { get; set; }
            public Followers followers { get; set; }
            public string href { get; set; }
            public string id { get; set; }
            public List<Image> images { get; set; }
            public string name { get; set; }
            public Owner owner { get; set; }
            public object primary_color { get; set; }
            public bool @public { get; set; }
            public string snapshot_id { get; set; }
            public Tracks tracks { get; set; }
            public string type { get; set; }
            public string uri { get; set; }
        }

        public class Track
        {
            public object preview_url { get; set; }
            public List<string> available_markets { get; set; }
            public bool @explicit { get; set; }
            public string type { get; set; }
            public bool episode { get; set; }
            public bool track { get; set; }
            public Album album { get; set; }
            public List<Artist> artists { get; set; }
            public int disc_number { get; set; }
            public int track_number { get; set; }
            public int duration_ms { get; set; }
            public ExternalIds external_ids { get; set; }
            public ExternalUrls external_urls { get; set; }
            public string href { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public int popularity { get; set; }
            public string uri { get; set; }
            public bool is_local { get; set; }
        }

        public class Tracks
        {
            public string href { get; set; }
            public List<Item> items { get; set; }
            public int limit { get; set; }
            public string next { get; set; }
            public int offset { get; set; }
            public object previous { get; set; }
            public int total { get; set; }
        }

        public class VideoThumbnail
        {
            public object url { get; set; }
        }
    }
}