using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

using log4net;

namespace SpotifyPlayListDownloader.Services
{
    public class DownloaderService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DownloaderService));

        private readonly string ytDlpPath;
        private readonly string ffmpegPath;

        public DownloaderService()
        {
            Log.Debug("Initializing DownloaderService");
            ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exe", "yt-dlp.exe");
            ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exe", "ffmpeg.exe");
            Log.Info($"yt-dlp path: {ytDlpPath}");
            Log.Info($"ffmpeg path: {ffmpegPath}");
        }

        public async Task DownloadMp3Async(string query, string outputDirectory, SpotifyPlayListDownloader.Clases.PlayListTracks.Item item)
        {
            Log.Info($"Start download for: {query}");
            try
            {
                var path = Path.Combine(outputDirectory, query + ".mp3");

                // Sanitizar el nombre del archivo
                string sanitizedQuery = string.Join("_", query.Split(Path.GetInvalidFileNameChars()));
                string output = Path.Combine(outputDirectory, $"{sanitizedQuery}.mp3");

                if (File.Exists(path))
                {
                    Log.Info($"File already exists: {path}");
                    Console.WriteLine($"[yt-dlp] Success: {query}");
                    return;
                }

                string search = $"ytsearch1:{query}";
                string args = $"-x --audio-format mp3 --ffmpeg-location \"{ffmpegPath}\" -o \"{output}\" \"{search}\"";

                Log.Debug($"yt-dlp arguments: {args}");

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
                Log.Debug("yt-dlp process started");

                string stdOut = await process.StandardOutput.ReadToEndAsync();
                string stdErr = await process.StandardError.ReadToEndAsync();

                process.WaitForExit();
                Log.Debug("yt-dlp process finished");

                if (process.ExitCode != 0)
                {
                    Log.Error($"yt-dlp failed with code {process.ExitCode}. Error: {stdErr}");
                    Console.WriteLine($"[yt-dlp] Error: {stdErr}");
                }
                else
                {
                    Log.Info($"yt-dlp success for: {query}");
                    DateTime releaseDate;
                    if (!string.IsNullOrWhiteSpace(item.track.album.release_date) && DateTime.TryParse(item.track.album.release_date, out releaseDate)) { }
                    else releaseDate = DateTime.MinValue;

                    SetMp3Metadata(output, item.track.name, item.track.artists.Select(a => a.name).ToArray(), item.track.album.name, (uint)releaseDate.Year);
                    Console.WriteLine($"[yt-dlp] Success: {query}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in DownloadMp3Async for {query}: {ex.Message}", ex);
            }
        }

        public void SetMp3Metadata(string filePath, string title, string[] artist, string album, uint year)
        {
            Log.Debug($"Setting metadata for {filePath}");
            if (File.Exists(filePath) == false)
            {
                Log.Warn($"File not found for metadata: {filePath}");
                return;
            }
            try
            {
                var file = TagLib.File.Create(filePath);
                file.Tag.Title = title;
                file.Tag.Performers = artist;
                file.Tag.Album = album;
                file.Tag.Year = year;
                file.Save();
                Log.Info($"Metadata saved for {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error setting metadata for {filePath}: {ex.Message}", ex);
            }
        }
    }
}
