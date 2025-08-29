using System.Text.RegularExpressions;

namespace SpotifyPlayListDownloader.Helpers
{
    public static class SpotifyHelper
    {
        public static bool TryGetSpotifyPlaylistId(string input, out string playlistId)
        {
            playlistId = null;
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim();

            var uriMatch = Regex.Match(input, @"^spotify:playlist:([A-Za-z0-9]+)$",
                                       RegexOptions.IgnoreCase);
            if (uriMatch.Success)
            {
                playlistId = uriMatch.Groups[1].Value;
                return true;
            }

            var urlPattern =
                @"^https?://open\.spotify\.com/"
              + @"(?:(?:intl-[a-z\-]+)/)?"
              + @"(?:(?:embed/)?playlist|user/[^/]+/playlist)/"
              + @"([A-Za-z0-9]+)"
              + @"(?:[/?#].*)?$";

            var urlMatch = Regex.Match(input, urlPattern, RegexOptions.IgnoreCase);
            if (urlMatch.Success)
            {
                playlistId = urlMatch.Groups[1].Value;
                return true;
            }

            return false;
        }

        public static bool TryGetSpotifyArtistId(string input, out string artistId)
        {
            artistId = null;
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim();

            var uriMatch = Regex.Match(input, @"^spotify:artist:([A-Za-z0-9]+)$",
                                       RegexOptions.IgnoreCase);
            if (uriMatch.Success)
            {
                artistId = uriMatch.Groups[1].Value;
                return true;
            }

            var urlPattern =
                @"^https?://open\.spotify\.com/"
              + @"(?:(?:intl-[a-z\-]+)/)?"
              + @"(?:(?:embed/)?artist)/"
              + @"([A-Za-z0-9]+)"
              + @"(?:[/?#].*)?$";

            var urlMatch = Regex.Match(input, urlPattern, RegexOptions.IgnoreCase);
            if (urlMatch.Success)
            {
                artistId = urlMatch.Groups[1].Value;
                return true;
            }

            return false;
        }
    }
}