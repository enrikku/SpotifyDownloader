using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace SpotifyPlayListDownloader.Services
{
    public class DownloaderService
    {
        private readonly string ytDlpPath;
        private readonly string ffmpegPath;

        public DownloaderService()
        {
            ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exe", "yt-dlp.exe");
            ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exe", "ffmpeg.exe");
        }

        public async Task DownloadMp3Async(string query, string outputDirectory, SpotifyPlayListDownloader.Clases.PlayListTracks.Item item)
        {
            try
            {
                var path = Path.Combine(outputDirectory, query + ".mp3");

                // Sanitizar el nombre del archivo
                string sanitizedQuery = string.Join("_", query.Split(Path.GetInvalidFileNameChars()));
                string output = Path.Combine(outputDirectory, $"{sanitizedQuery}.mp3");

                if (File.Exists(path))
                {
                    Console.WriteLine($"[yt-dlp] Success: {query}");
                    return;
                }

                string search = $"ytsearch1:{query}";

                string args = $"-x --audio-format mp3 --ffmpeg-location \"{ffmpegPath}\" -o \"{output}\" \"{search}\"";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ytDlpPath,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };

                process.Start();

                string stdOut = await process.StandardOutput.ReadToEndAsync();
                string stdErr = await process.StandardError.ReadToEndAsync();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"[yt-dlp] Error: {stdErr}");
                    MessageBox.Show($"Error: {stdErr}");
                }
                else
                {
                    DateTime releaseDate;
                    if (!string.IsNullOrWhiteSpace(item.track.album.release_date) && DateTime.TryParse(item.track.album.release_date, out releaseDate)) { }
                    else releaseDate = DateTime.MinValue;

                    SetMp3Metadata(output, item.track.name, item.track.artists.Select(a => a.name).ToArray(), item.track.album.name, (uint)releaseDate.Year);
                    Console.WriteLine($"[yt-dlp] Success: {query}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        public void SetMp3Metadata(string filePath, string title, string[] artist, string album, uint year)
        {
            if (File.Exists(filePath) == false) return;
            var file = TagLib.File.Create(filePath);
            file.Tag.Title = title;
            file.Tag.Performers = artist;
            file.Tag.Album = album;
            file.Tag.Year = year;
            file.Save();
        }
    }
}