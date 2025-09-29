namespace SpotifyPlayListDownloader.Helpers
{
    public static class PathHelper
    {
        public static string SanitizeSimple(string name, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "_";

            var invalid = Path.GetInvalidFileNameChars();
            foreach (var ch in invalid)
                name = name.Replace(ch, '_');

            name = name.Trim();

            if (name.Length > maxLength)
                name = name.Substring(0, maxLength);

            return name;
        }
    }
}