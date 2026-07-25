# music-lyrics

`music-lyrics` is the separated frontend/backend migration target for `163MusicLyrics-master`.

## Stack

- `api`: ASP.NET Core Minimal API on .NET 10
- `web`: Vue 3 + Vite + TypeScript

## API

The backend exposes:

- `GET /api/health`
- `POST /api/search`
- `POST /api/tracks`
- `POST /api/lyrics`
- `POST /api/song-link`
- `POST /api/convert`

It supports NetEase Cloud Music and QQ Music search, direct ID/link lookup, lyric retrieval, LRC/SRT output, and LRC/SRT conversion. Cookie fields are optional and can be supplied when a provider requires login.

## Run

Start the API:

```powershell
cd api
dotnet run --urls http://localhost:5000
```

Start the web app:

```powershell
cd web
npm run dev
```

Vite proxies `/api` to `http://localhost:5000` during development.

## Build

```powershell
cd api
dotnet build

cd ..\web
npm run build
```
