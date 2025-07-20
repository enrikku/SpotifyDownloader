# 🎵 Spotify Playlist Downloader

Una aplicación de escritorio en C# (WPF) que:

- Se conecta a la API de Spotify para obtener las canciones de una playlist.
- Busca cada canción en YouTube.
- Descarga el audio en formato MP3 usando `yt-dlp` y `ffmpeg`.

---

## 🚀 Requisitos

### 📦 Software necesario

| Componente      | Versión mínima | Uso                        |
|-----------------|----------------|----------------------------|
| .NET SDK        | 6.0 o superior | Compilar y ejecutar        |
| yt-dlp.exe      | Última         | Buscar y extraer el audio  |
| ffmpeg.exe      | Última         | Convertir a MP3            |

> ✅ Coloca `yt-dlp.exe` y `ffmpeg.exe` en la misma carpeta que el `.exe` generado o en `bin/Debug/...`

---

## 🔐 Configuración de claves

Para usar la API de Spotify necesitas un `Client ID` y `Client Secret`.

### 1. Registra tu app en [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)

- Crea una app
- Copia tu `Client ID` y `Client Secret`

### 2. Crea un archivo `appsettings.json` en la raíz del proyecto

```json
{
  "Spotify": {
    "ClientId": "TU_CLIENT_ID",
    "ClientSecret": "TU_CLIENT_SECRET"
  }
}
