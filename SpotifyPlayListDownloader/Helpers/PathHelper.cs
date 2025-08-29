namespace SpotifyPlayListDownloader.Helpers
{
    public static class PathHelper
    {
        public static string SanitizeSimple(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "_";

            var invalid = Path.GetInvalidFileNameChars();
            foreach (var ch in invalid)
                name = name.Replace(ch, '_');

            return name.Trim();
        }
    }
}