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

        public bool DownloadMp3(TrackInfo track)
        {
            // Validaciones básicas
            if (track == null)
            {
                Log.Error("DownloadMp3: track es null.");
                return false;
            }

            // Construir query segura
            var primaryArtist = (track.artists != null && track.artists.Count > 0)
                ? track.artists[0]
                : "Unknown Artist";

            var query = $"{primaryArtist} - {track.name}".Trim();
            if (string.IsNullOrWhiteSpace(query))
                query = track.name ?? "Unknown Track";

            var output = track.path;
            if (string.IsNullOrWhiteSpace(output))
            {
                Log.Error($"DownloadMp3: track.path es null o vacío. Query: {query}");
                return false;
            }

            try
            {
                // Asegurar carpeta
                var outDir = System.IO.Path.GetDirectoryName(output);
                if (!string.IsNullOrWhiteSpace(outDir) && !Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                // Si ya existe, salir
                if (File.Exists(output))
                {
                    Log.Info($"El archivo ya existe: {output}");
                    return true;
                }

                // Helpers
                static string Q(string s) => $"\"{s}\"";

                // yt-dlp args
                string search = $"ytsearch1:{query}";
                string args =
                    string.Join(" ", new[]
                    {
                "-x",
                "--audio-format", "mp3",
                "--no-playlist",
                "--ignore-errors",
                "--restrict-filenames",
                "--ffmpeg-location", Q(ffmpegPath),
                "-o", Q(output),
                Q(search)
                    });

                // Verificación básica de binarios
                if (!File.Exists(ytDlpPath))
                {
                    Log.Error($"yt-dlp no encontrado en: {ytDlpPath}");
                    return false;
                }
                if (!File.Exists(ffmpegPath))
                {
                    Log.Error($"ffmpeg no encontrado en: {ffmpegPath}");
                    return false;
                }

                Log.Info($"Empezando descarga: {query}");
                Log.Debug($"yt-dlp argumentos: {args}");

                var psi = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                var stdOutSb = new StringBuilder();
                var stdErrSb = new StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stdOutSb.AppendLine(e.Data);
                        // Opcional: parsear progreso si quisieras
                        // Log.Debug($"yt-dlp: {e.Data}");
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stdErrSb.AppendLine(e.Data);
                        // Muchos mensajes de progreso salen por stderr en yt-dlp
                        // Log.Debug($"yt-dlp[stderr]: {e.Data}");
                    }
                };

                var start = DateTime.Now;

                if (!process.Start())
                {
                    Log.Error("No se pudo iniciar el proceso yt-dlp.");
                    return false;
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Timeout (ej. 10 minutos)
                const int timeoutMs = 10 * 60 * 1000;
                bool exited = process.WaitForExit(timeoutMs);

                if (!exited)
                {
                    try { process.Kill(entireProcessTree: true); } catch { /* ignorar */ }
                    Log.Error($"yt-dlp timeout tras {timeoutMs / 1000} s. Query: {query}");
                    return false;
                }

                // Asegurar leer todo
                process.WaitForExit();

                var elapsed = DateTime.Now - start;
                string elapsedFormatted = $"{(int)elapsed.TotalHours:00}h {elapsed.Minutes:00}m {elapsed.Seconds:00}s";

                if (process.ExitCode != 0)
                {
                    Log.Error($"yt-dlp ha fallado (código {process.ExitCode}) en {elapsedFormatted}. " +
                              $"Error: {stdErrSb.ToString().Trim()}. Query: {query}");
                    return false;
                }

                Log.Info($"yt-dlp descarga exitosa en {elapsedFormatted}: {query}");

                // Etiquetas MP3
                try
                {
                    SetMp3Metadata2(track);
                }
                catch (Exception tagEx)
                {
                    Log.Error($"Error al asignar metadatos MP3 para {query}: {tagEx.Message}", tagEx);
                    // No consideramos esto un fallo de la descarga
                }

                // Verificación final de archivo
                if (!File.Exists(output))
                {
                    Log.Error($"Descarga reportada exitosa pero no se encontró el archivo: {output}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Excepción en DownloadMp3 para '{query}': {ex.Message}", ex);
                return false;
            }
        }

        public void SetMp3Metadata2(TrackInfo track)
        {
            Log.Debug($"Estableciendo metadatos para {track.path}");
            if (File.Exists(track.path) == false)
            {
                Log.Warn($"Fichero no encontrado para establecer metadatos: {track.path}");
                return;
            }
            try
            {
                var file = TagLib.File.Create(track.path);
                file.Tag.Title = track.name;
                file.Tag.Performers = new string[] { string.Join(", ", track.artists) };
                file.Tag.Album = track.album;
                file.Tag.Year = track.year;
                file.Save();
                Log.Info($"Metadatos establecidos para {track.path}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error estableciendo metadatos para {track.path}: {ex.Message}", ex);
            }
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