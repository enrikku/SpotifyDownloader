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

        public async Task DownloadMp3Async(string query, string outputDirectory)
        {
            try
            {
                string search = $"ytsearch1:{query}";
                string output = Path.Combine(outputDirectory, "%(title)s.%(ext)s");

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
                else Console.WriteLine($"[yt-dlp] Success: {query}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                return;
            }
        }
    }
}