namespace MusicLyrics.Api;

public sealed record SearchRequest(
    string Keyword,
    string Provider = "netease",
    string Type = "song",
    string? NetEaseCookie = null,
    string? QqCookie = null);

public sealed record ContainerTracksRequest(
    string Provider,
    string Type,
    string Id,
    string? NetEaseCookie = null,
    string? QqCookie = null);

public sealed record LyricsRequest(
    string Provider,
    string SongId,
    string Format = "lrc",
    bool IncludeTranslation = true,
    bool IncludeTransliteration = false,
    string? NetEaseCookie = null,
    string? QqCookie = null);

public sealed record SongLinkRequest(
    string Provider,
    string SongId,
    string? NetEaseCookie = null,
    string? QqCookie = null);

public sealed record ConvertLyricsRequest(
    string Input,
    string From,
    string To,
    long DurationMs = 0);

public sealed record SearchResponse(IReadOnlyList<SearchItem> Items);

public sealed record TrackResponse(IReadOnlyList<SongInfo> Tracks);

public sealed record LyricsResponse(
    SongInfo Song,
    RawLyrics Raw,
    string Format,
    string Output);

public sealed record SongLinkResponse(string Url, string Source);

public sealed record ConvertLyricsResponse(string Output);

public sealed record ErrorResponse(string Message);

public sealed record SearchItem(
    string Provider,
    string Type,
    string Id,
    string Title,
    IReadOnlyList<string> Artists,
    string? Album,
    long? DurationMs,
    string? CoverUrl,
    long? SongCount,
    string? Description);

public sealed record SongInfo(
    string Provider,
    string Id,
    string DisplayId,
    string Title,
    IReadOnlyList<string> Artists,
    string Album,
    long DurationMs,
    string? CoverUrl,
    string? PublishDate);

public sealed record RawLyrics(
    string Lyric,
    string Translation,
    string Transliteration)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Lyric)
        && string.IsNullOrWhiteSpace(Translation)
        && string.IsNullOrWhiteSpace(Transliteration);
}

public enum ProviderKind
{
    NetEase,
    QQ
}

public enum SearchKind
{
    Song,
    Album,
    Playlist
}

public sealed class ApiException(string message) : Exception(message);
