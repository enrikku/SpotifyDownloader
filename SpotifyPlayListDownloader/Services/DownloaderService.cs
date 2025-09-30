using System.Collections.Concurrent;
using TagLib;
using File = System.IO.File;

namespace SpotifyDownloader.Services
{
    public class DownloaderService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DownloaderService));

        private readonly string ytDlpPath;
        private readonly string ffmpegPath;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

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

        public async Task SetMp3Metadata2(TrackInfo track)
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
                file.Tag.Performers = track.artists.ToArray();

                if (track.artists.Count > 0)
                {
                    var artists = track.artists.ToArray();
                    var otherArtists = track.artists.Skip(1).ToArray();

                    file.Tag.Performers = artists;
                    file.Tag.AlbumArtists = new string[] { artists[0] };
                }

                file.Tag.Album = track.album;
                file.Tag.Year = track.year;

                var picture = await GetCachedPictureAsync(track.albumImage);
                if (picture != null)
                    file.Tag.Pictures = new IPicture[] { picture };

                if (track.artist != null)
                {
                    if (track.artist.genres.Count > 0)
                    {
                        file.Tag.Genres = track.artist.genres.ToArray();
                    }
                }

                file.Save();
                Log.Info($"Metadatos establecidos para {track.path}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error estableciendo metadatos para {track.path}: {ex.Message}", ex);
            }
        }

        private static string GuessMimeFromUrl(string url)
        {
            var ext = Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }

        private async Task<IPicture?> GetCachedPictureAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;

            var cacheDir = Path.Combine(AppContext.BaseDirectory, "image_cache");
            Directory.CreateDirectory(cacheDir);

            var hash = Convert.ToHexString(
                System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(imageUrl))
            );

            var ext = Path.GetExtension(new Uri(imageUrl).AbsolutePath);
            if (string.IsNullOrWhiteSpace(ext))
                ext = ".jpg";

            var fileName = Path.Combine(cacheDir, hash + ext);

            var semaphore = _locks.GetOrAdd(hash, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                byte[] bytes;
                string contentType;

                if (File.Exists(fileName))
                {
                    bytes = await File.ReadAllBytesAsync(fileName);
                    contentType = GuessMimeFromUrl(fileName);
                }
                else
                {
                    using var resp = await _http.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);
                    resp.EnsureSuccessStatusCode();

                    bytes = await resp.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(fileName, bytes);

                    contentType = resp.Content.Headers.ContentType?.MediaType
                                  ?? GuessMimeFromUrl(fileName);
                }

                return new TagLib.Picture
                {
                    Type = PictureType.FrontCover,
                    Description = "Cover",
                    MimeType = contentType,
                    Data = new TagLib.ByteVector(bytes)
                };
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}