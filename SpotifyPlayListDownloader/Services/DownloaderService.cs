namespace SpotifyPlayListDownloader.Services
{
    public class DownloaderService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DownloaderService));

        private readonly string ytDlpPath;
        private readonly string ffmpegPath;

        public DownloaderService()
        {
            Log.Debug("Iniciando DownloaderService");
            ytDlpPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exe", "yt-dlp.exe");
            ffmpegPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exe", "ffmpeg.exe");
            Log.Info($"yt-dlp ruta: {ytDlpPath}");
            Log.Info($"ffmpeg ruta: {ffmpegPath}");
        }

        public async Task DownloadMp3Async(string query, string title, string outputDirectory, object item, string? albumName, AlbumSongs.Root? album = null)
        {
            Log.Info($"Emepzando descarga: {query}");
            try
            {
                // Sanitizar el nombre del archivo
                string sanitizedTitle = string.Join("_", title.Split(System.IO.Path.GetInvalidFileNameChars()));
                string output = System.IO.Path.Combine(outputDirectory, $"{sanitizedTitle}.mp3");

                if (File.Exists(output))
                {
                    Log.Info($"El archivo ya existe: {output}");
                    return;
                }

                string search = $"ytsearch1:{query}";
                string args = $"-x --audio-format mp3 --ffmpeg-location \"{ffmpegPath}\" -o \"{output}\" \"{search}\"";

                Log.Debug($"yt-dlp argumentos: {args}");

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
                Log.Debug("yt-dlp proceso iniciado");

                string stdOut = await process.StandardOutput.ReadToEndAsync();
                string stdErr = await process.StandardError.ReadToEndAsync();

                process.WaitForExit();
                Log.Debug("yt-dlp proceso finalizado");

                if (process.ExitCode != 0)
                {
                    Log.Error($"yt-dlp a falldo con codigo de salida {process.ExitCode}. Error: {stdErr}. Query: {query}");
                }
                else
                {
                    if (item is SpotifyPlayListDownloader.Clases.PlayListTracks.Item item2)
                    {
                        Log.Info($"yt-dlp descarga exitosa: {query}");
                        DateTime releaseDate;
                        if (!string.IsNullOrWhiteSpace(item2.track.album.release_date) && DateTime.TryParse(item2.track.album.release_date, out releaseDate)) { }
                        else releaseDate = DateTime.MinValue;

                        SetMp3Metadata(output, item2.track.name, item2.track.artists.Select(a => a.name).ToArray(), item2.track.album.name, (uint)releaseDate.Year);
                    }
                    else if (item is AlbumSongs.Item item3)
                    {
                        Log.Info($"yt-dlp descarga exitosa: {query}");

                        uint date = 0;

                        if (album != null)
                        {
                            var year = album.release_date.Split("-")[0];
                            if (uint.TryParse(year, out date)) { }
                            else date = 0;
                        }

                        SetMp3Metadata(output, item3.name, item3.artists.Select(a => a.name).ToArray(), albumName, date);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in DownloadMp3Async for {query}: {ex.Message}", ex);
            }
        }

        public void SetMp3Metadata(string filePath, string title, string[] artist, string album, uint year)
        {
            Log.Debug($"Estableciendo metadatos para {filePath}");
            if (File.Exists(filePath) == false)
            {
                Log.Warn($"Fichero no encontrado para establecer metadatos: {filePath}");
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
                Log.Info($"Metadatos establecidos para {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error estableciendo metadatos para {filePath}: {ex.Message}", ex);
            }
        }
    }
}