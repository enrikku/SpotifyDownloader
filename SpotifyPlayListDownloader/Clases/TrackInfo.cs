namespace SpotifyPlayListDownloader.Clases
{
    public class TrackInfo
    {
        public string name = "";
        public List<string> artists = new List<string>();
        public string album = "";
        public uint year = 0;
        public bool isDownloaded = false;
        public string path = "";
        public string albumImage = "";
    }
}