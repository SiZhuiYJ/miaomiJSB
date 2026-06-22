using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MusicLyrics.Api;

public sealed class MusicService
{
    private readonly NetEaseMusicProvider _netEase = new();
    private readonly QqMusicProvider _qq = new();

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            throw new ApiException("请输入搜索关键词。");
        }

        var type = ParseSearchKind(request.Type);
        var providers = ParseProviders(request.Provider);
        var keyword = request.Keyword.Trim();

        if (providers.Count == 1 && ShouldUseDirectLookup(providers[0], type, keyword))
        {
            return new SearchResponse(await DirectLookupAsync(providers[0], type, keyword, request, ct));
        }

        var items = new List<SearchItem>();

        foreach (var provider in providers)
        {
            var providerItems = provider switch
            {
                ProviderKind.NetEase => await _netEase.SearchAsync(keyword, type, request.NetEaseCookie, ct),
                ProviderKind.QQ => await _qq.SearchAsync(keyword, type, request.QqCookie, ct),
                _ => []
            };

            items.AddRange(providerItems);
        }

        return new SearchResponse(items);
    }

    private async Task<IReadOnlyList<SearchItem>> DirectLookupAsync(
        ProviderKind provider,
        SearchKind type,
        string input,
        SearchRequest request,
        CancellationToken ct)
    {
        var id = NormalizeInputId(provider, type, input);

        if (type == SearchKind.Song)
        {
            var song = provider switch
            {
                ProviderKind.NetEase => await _netEase.GetSongAsync(id, request.NetEaseCookie, ct),
                ProviderKind.QQ => await _qq.GetSongAsync(id, request.QqCookie, ct),
                _ => throw new ApiException("不支持的音乐平台。")
            };

            return
            [
                new SearchItem(
                    ProviderToApiName(provider),
                    "song",
                    song.DisplayId,
                    song.Title,
                    song.Artists,
                    song.Album,
                    song.DurationMs,
                    song.CoverUrl,
                    null,
                    null)
            ];
        }

        var tracks = provider switch
        {
            ProviderKind.NetEase => await _netEase.GetContainerTracksAsync(id, type, request.NetEaseCookie, ct),
            ProviderKind.QQ => await _qq.GetContainerTracksAsync(id, type, request.QqCookie, ct),
            _ => throw new ApiException("不支持的音乐平台。")
        };

        var firstTrack = tracks.FirstOrDefault();
        return
        [
            new SearchItem(
                ProviderToApiName(provider),
                SearchTypeToApiName(type),
                id,
                $"{(type == SearchKind.Album ? "专辑" : "歌单")} {id}",
                [],
                null,
                null,
                firstTrack?.CoverUrl,
                tracks.Count,
                "精确 ID/链接查询结果")
        ];
    }

    public async Task<TrackResponse> GetContainerTracksAsync(ContainerTracksRequest request, CancellationToken ct)
    {
        var provider = ParseProvider(request.Provider);
        var type = ParseSearchKind(request.Type);
        var id = NormalizeInputId(provider, type, request.Id);

        if (type == SearchKind.Song)
        {
            throw new ApiException("单曲不需要展开曲目。");
        }

        var tracks = provider switch
        {
            ProviderKind.NetEase => await _netEase.GetContainerTracksAsync(id, type, request.NetEaseCookie, ct),
            ProviderKind.QQ => await _qq.GetContainerTracksAsync(id, type, request.QqCookie, ct),
            _ => []
        };

        return new TrackResponse(tracks);
    }

    public async Task<LyricsResponse> GetLyricsAsync(LyricsRequest request, CancellationToken ct)
    {
        var provider = ParseProvider(request.Provider);
        var songId = NormalizeInputId(provider, SearchKind.Song, request.SongId);

        var song = provider switch
        {
            ProviderKind.NetEase => await _netEase.GetSongAsync(songId, request.NetEaseCookie, ct),
            ProviderKind.QQ => await _qq.GetSongAsync(songId, request.QqCookie, ct),
            _ => throw new ApiException("不支持的音乐平台。")
        };

        var raw = provider switch
        {
            ProviderKind.NetEase => await _netEase.GetLyricsAsync(song.DisplayId, request.NetEaseCookie, ct),
            ProviderKind.QQ => await _qq.GetLyricsAsync(song.DisplayId, request.QqCookie, ct),
            _ => throw new ApiException("不支持的音乐平台。")
        };

        if (raw.IsEmpty)
        {
            throw new ApiException("未查询到歌词。");
        }

        var output = LyricsFormatter.Format(
            raw,
            request.Format,
            song.DurationMs,
            request.IncludeTranslation,
            request.IncludeTransliteration);

        return new LyricsResponse(song, raw, request.Format.ToLowerInvariant(), output);
    }

    public async Task<SongLinkResponse> GetSongLinkAsync(SongLinkRequest request, CancellationToken ct)
    {
        var provider = ParseProvider(request.Provider);
        var songId = NormalizeInputId(provider, SearchKind.Song, request.SongId);

        var link = provider switch
        {
            ProviderKind.NetEase => await _netEase.GetSongLinkAsync(songId, request.NetEaseCookie, ct),
            ProviderKind.QQ => await _qq.GetSongLinkAsync(songId, request.QqCookie, ct),
            _ => new SongLinkResponse("", "")
        };

        if (string.IsNullOrWhiteSpace(link.Url))
        {
            throw new ApiException("未获取到试听链接。");
        }

        return link;
    }

    private static ProviderKind ParseProvider(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "netease" or "net_ease_music" or "163" => ProviderKind.NetEase,
            "qq" or "qq_music" => ProviderKind.QQ,
            _ => throw new ApiException("不支持的音乐平台。")
        };
    }

    private static IReadOnlyList<ProviderKind> ParseProviders(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "all" => [ProviderKind.NetEase, ProviderKind.QQ],
            _ => [ParseProvider(value)]
        };
    }

    private static SearchKind ParseSearchKind(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "song" or "song_id" => SearchKind.Song,
            "album" or "album_id" => SearchKind.Album,
            "playlist" or "playlist_id" => SearchKind.Playlist,
            _ => throw new ApiException("不支持的搜索类型。")
        };
    }

    private static bool ShouldUseDirectLookup(ProviderKind provider, SearchKind type, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (input.Contains("music.163.com", StringComparison.OrdinalIgnoreCase)
            || input.Contains("y.qq.com", StringComparison.OrdinalIgnoreCase)
            || input.Contains("c.y.qq.com", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (provider == ProviderKind.NetEase)
        {
            return input.All(char.IsDigit);
        }

        // QQ 的 MID 是字母数字混合，和普通英文关键词冲突很大；只有明显链接才自动精确查询。
        return type != SearchKind.Song && input.All(char.IsDigit);
    }

    private static string ProviderToApiName(ProviderKind provider)
    {
        return provider switch
        {
            ProviderKind.QQ => "qq",
            _ => "netease"
        };
    }

    private static string SearchTypeToApiName(SearchKind type)
    {
        return type switch
        {
            SearchKind.Album => "album",
            SearchKind.Playlist => "playlist",
            _ => "song"
        };
    }

    private static string NormalizeInputId(ProviderKind provider, SearchKind type, string input)
    {
        var value = input.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApiException("请输入 ID 或链接。");
        }

        if (provider == ProviderKind.NetEase)
        {
            var keyword = type switch
            {
                SearchKind.Song => "song",
                SearchKind.Album => "album",
                SearchKind.Playlist => "playlist",
                _ => ""
            };
            var match = Regex.Match(value, $@"{keyword}\?id=(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : value;
        }

        var patterns = type switch
        {
            SearchKind.Song => new[]
            {
                @"songDetail/([A-Za-z0-9]+)",
                @"playsong\.html\?songid=([A-Za-z0-9]+)"
            },
            SearchKind.Album => new[]
            {
                @"albumDetail/([A-Za-z0-9]+)",
                @"album\.html\?albummid=([A-Za-z0-9]+)",
                @"album\.html\?.*albumId=([A-Za-z0-9]+)"
            },
            SearchKind.Playlist => new[]
            {
                @"playlist/([A-Za-z0-9]+)",
                @"taoge\.html\?id=([A-Za-z0-9]+)"
            },
            _ => []
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(value, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return value;
    }
}

internal sealed class NetEaseMusicProvider
{
    private const string Provider = "netease";
    private const string Referer = "https://music.163.com/";
    private const string Modulus = "00e0b509f6259df8642dbc35662901477df22677ec152b5ff68ace615bb7b725152b3ab17a876aea8a5aa76d2e417629ec4ee341f56135fccf695280104e0312ecbda92557c93870114af6c9d05c4f7f0c3685b7a46bee255932575cce10b424d813cfe4875d3e82047b97ddef52741d546b8e289dc6935b3ece0462db0a22b8e7";
    private const string Nonce = "0CoJUm6Qyw8W8jud";
    private const string PubKey = "010001";
    private const string Vi = "0102030405060708";

    private readonly HttpClient _client = HttpHelpers.CreateClient();
    private readonly string _secretKey = CreateSecretKey(16);
    private readonly string _defaultCookie = "NMTID=" + Guid.NewGuid();

    public async Task<IReadOnlyList<SearchItem>> SearchAsync(string keyword, SearchKind type, string? cookie, CancellationToken ct)
    {
        var typeCode = type switch
        {
            SearchKind.Song => "1",
            SearchKind.Album => "10",
            SearchKind.Playlist => "1000",
            _ => "1"
        };

        using var doc = await PostWeApiAsync("https://music.163.com/weapi/cloudsearch/get/web", new Dictionary<string, object?>
        {
            ["csrf_token"] = "",
            ["s"] = keyword,
            ["type"] = typeCode,
            ["limit"] = "20",
            ["offset"] = "0"
        }, cookie, ct);

        var root = doc.RootElement;
        var code = JsonHelpers.GetString(root, "code");
        if (code == "50000005")
        {
            throw new ApiException("网易云接口需要登录 Cookie。");
        }

        if (code != "200" || !root.TryGetProperty("result", out var result))
        {
            return [];
        }

        return type switch
        {
            SearchKind.Song => ParseSongSearch(result),
            SearchKind.Album => ParseAlbumSearch(result),
            SearchKind.Playlist => ParsePlaylistSearch(result),
            _ => []
        };
    }

    public async Task<SongInfo> GetSongAsync(string songId, string? cookie, CancellationToken ct)
    {
        var songs = await GetSongsAsync([songId], cookie, ct);
        return songs.FirstOrDefault() ?? throw new ApiException("未查询到歌曲信息。");
    }

    public async Task<IReadOnlyList<SongInfo>> GetContainerTracksAsync(string id, SearchKind type, string? cookie, CancellationToken ct)
    {
        if (type == SearchKind.Album)
        {
            using var doc = await PostWeApiAsync($"https://music.163.com/weapi/v1/album/{id}?csrf_token=", new Dictionary<string, object?>
            {
                ["csrf_token"] = ""
            }, cookie, ct);

            if (JsonHelpers.GetString(doc.RootElement, "code") != "200")
            {
                throw new ApiException("未查询到专辑信息。");
            }

            return ParseSongs(doc.RootElement.GetProperty("songs"));
        }

        using var playlistDoc = await PostWeApiAsync("https://music.163.com/weapi/v6/playlist/detail?csrf_token=", new Dictionary<string, object?>
        {
            ["csrf_token"] = "",
            ["id"] = id,
            ["offset"] = "0",
            ["total"] = "true",
            ["limit"] = "1000",
            ["n"] = "1000"
        }, cookie, ct);

        if (JsonHelpers.GetString(playlistDoc.RootElement, "code") == "20001")
        {
            throw new ApiException("该歌单需要网易云登录 Cookie。");
        }

        if (!playlistDoc.RootElement.TryGetProperty("playlist", out var playlist)
            || !playlist.TryGetProperty("trackIds", out var trackIds))
        {
            throw new ApiException("未查询到歌单信息。");
        }

        var ids = trackIds.EnumerateArray()
            .Select(x => JsonHelpers.GetString(x, "id"))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(1000)
            .ToArray();

        return await GetSongsAsync(ids!, cookie, ct);
    }

    public async Task<RawLyrics> GetLyricsAsync(string songId, string? cookie, CancellationToken ct)
    {
        using var doc = await PostWeApiAsync("https://music.163.com/weapi/song/lyric?csrf_token=", new Dictionary<string, object?>
        {
            ["id"] = songId,
            ["os"] = "pc",
            ["lv"] = "-1",
            ["kv"] = "-1",
            ["tv"] = "-1",
            ["rv"] = "-1",
            ["yv"] = "-1",
            ["ytv"] = "-1",
            ["yrv"] = "-1",
            ["csrf_token"] = ""
        }, cookie, ct);

        if (JsonHelpers.GetString(doc.RootElement, "code") != "200")
        {
            return new RawLyrics("", "", "");
        }

        return new RawLyrics(
            JsonHelpers.GetNestedString(doc.RootElement, "lrc", "lyric"),
            JsonHelpers.GetNestedString(doc.RootElement, "tlyric", "lyric"),
            JsonHelpers.GetNestedString(doc.RootElement, "romalrc", "lyric"));
    }

    public async Task<SongLinkResponse> GetSongLinkAsync(string songId, string? cookie, CancellationToken ct)
    {
        var url = await TryGetPlayerUrlV1Async(songId, cookie, ct);
        if (!string.IsNullOrWhiteSpace(url))
        {
            return new SongLinkResponse(url, "netease-player-url-v1");
        }

        try
        {
            using var doc = await PostWeApiAsync("https://music.163.com/weapi/song/enhance/player/url?csrf_token=", new Dictionary<string, object?>
            {
                ["ids"] = $"[{songId}]",
                ["br"] = "999000",
                ["csrf_token"] = ""
            }, cookie, ct);

            if (JsonHelpers.GetString(doc.RootElement, "code") != "200"
                || !doc.RootElement.TryGetProperty("data", out var data)
                || data.ValueKind != JsonValueKind.Array
                || data.GetArrayLength() == 0)
            {
                return NetEaseOuterUrl(songId);
            }

            url = JsonHelpers.GetString(data[0], "url");
            return string.IsNullOrWhiteSpace(url)
                ? NetEaseOuterUrl(songId)
                : new SongLinkResponse(url, "netease-player-url");
        }
        catch
        {
            return NetEaseOuterUrl(songId);
        }
    }

    private async Task<string> TryGetPlayerUrlV1Async(string songId, string? cookie, CancellationToken ct)
    {
        try
        {
            using var doc = await PostWeApiAsync("https://music.163.com/weapi/song/enhance/player/url/v1?csrf_token=", new Dictionary<string, object?>
            {
                ["ids"] = $"[{songId}]",
                ["level"] = "standard",
                ["encodeType"] = "mp3",
                ["csrf_token"] = ""
            }, cookie, ct);

            if (JsonHelpers.GetString(doc.RootElement, "code") != "200"
                || !doc.RootElement.TryGetProperty("data", out var data)
                || data.ValueKind != JsonValueKind.Array
                || data.GetArrayLength() == 0)
            {
                return "";
            }

            return JsonHelpers.GetString(data[0], "url");
        }
        catch
        {
            return "";
        }
    }

    private static SongLinkResponse NetEaseOuterUrl(string songId)
    {
        return new SongLinkResponse($"https://music.163.com/song/media/outer/url?id={WebUtility.UrlEncode(songId)}.mp3", "netease-outer-url");
    }

    private async Task<IReadOnlyList<SongInfo>> GetSongsAsync(IEnumerable<string> songIds, string? cookie, CancellationToken ct)
    {
        var result = new List<SongInfo>();
        foreach (var batch in songIds.Where(x => !string.IsNullOrWhiteSpace(x)).Chunk(300))
        {
            var c = JsonSerializer.Serialize(batch.Select(id => new { id }));
            using var doc = await PostWeApiAsync("https://music.163.com/weapi/v3/song/detail?csrf_token=", new Dictionary<string, object?>
            {
                ["c"] = c,
                ["csrf_token"] = ""
            }, cookie, ct);

            if (JsonHelpers.GetString(doc.RootElement, "code") == "200"
                && doc.RootElement.TryGetProperty("songs", out var songs))
            {
                result.AddRange(ParseSongs(songs));
            }
        }

        return result;
    }

    private async Task<JsonDocument> PostWeApiAsync(string url, Dictionary<string, object?> data, string? cookie, CancellationToken ct)
    {
        var raw = JsonSerializer.Serialize(data);
        var form = Prepare(raw);
        var body = await HttpHelpers.PostFormAsync(_client, url, form, Referer, string.IsNullOrWhiteSpace(cookie) ? _defaultCookie : cookie, ct);
        return JsonDocument.Parse(body);
    }

    private Dictionary<string, string> Prepare(string raw)
    {
        var first = AesEncrypt(raw, Nonce);
        var second = AesEncrypt(first, _secretKey);

        return new Dictionary<string, string>
        {
            ["params"] = second,
            ["encSecKey"] = RsaEncode(_secretKey)
        };
    }

    private static IReadOnlyList<SearchItem> ParseSongSearch(JsonElement result)
    {
        if (!result.TryGetProperty("songs", out var songs) || songs.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return songs.EnumerateArray()
            .Select(song => new SearchItem(
                Provider,
                "song",
                JsonHelpers.GetString(song, "id"),
                JsonHelpers.GetString(song, "name"),
                JsonHelpers.ReadArtists(song, "ar"),
                JsonHelpers.GetNestedString(song, "al", "name"),
                JsonHelpers.GetLong(song, "dt"),
                JsonHelpers.GetNestedString(song, "al", "picUrl"),
                null,
                null))
            .ToArray();
    }

    private static IReadOnlyList<SearchItem> ParseAlbumSearch(JsonElement result)
    {
        if (!result.TryGetProperty("albums", out var albums) || albums.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return albums.EnumerateArray()
            .Select(album => new SearchItem(
                Provider,
                "album",
                JsonHelpers.GetString(album, "id"),
                JsonHelpers.GetString(album, "name"),
                JsonHelpers.ReadArtists(album, "artists"),
                null,
                null,
                JsonHelpers.GetString(album, "picUrl"),
                JsonHelpers.GetLong(album, "size"),
                null))
            .ToArray();
    }

    private static IReadOnlyList<SearchItem> ParsePlaylistSearch(JsonElement result)
    {
        if (!result.TryGetProperty("playlists", out var playlists) || playlists.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return playlists.EnumerateArray()
            .Select(playlist => new SearchItem(
                Provider,
                "playlist",
                JsonHelpers.GetString(playlist, "id"),
                JsonHelpers.GetString(playlist, "name"),
                [JsonHelpers.GetNestedString(playlist, "creator", "nickname")],
                null,
                null,
                JsonHelpers.GetString(playlist, "coverImgUrl"),
                JsonHelpers.GetLong(playlist, "trackCount"),
                JsonHelpers.GetString(playlist, "description")))
            .ToArray();
    }

    private static IReadOnlyList<SongInfo> ParseSongs(JsonElement songs)
    {
        if (songs.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return songs.EnumerateArray()
            .Select(song => new SongInfo(
                Provider,
                JsonHelpers.GetString(song, "id"),
                JsonHelpers.GetString(song, "id"),
                JsonHelpers.GetString(song, "name"),
                JsonHelpers.ReadArtists(song, "ar"),
                JsonHelpers.GetNestedString(song, "al", "name"),
                JsonHelpers.GetLong(song, "dt"),
                JsonHelpers.GetNestedString(song, "al", "picUrl"),
                FormatDate(JsonHelpers.GetLong(song, "publishTime"))))
            .ToArray();
    }

    private static string FormatDate(long ms)
    {
        if (ms <= 0)
        {
            return "";
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(ms).ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string AesEncrypt(string input, string secret)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(secret);
        aes.IV = Encoding.UTF8.GetBytes(Vi);
        aes.Mode = CipherMode.CBC;

        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(input);
        var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(encrypted);
    }

    private static string RsaEncode(string text)
    {
        var reversed = new string(text.Reverse().ToArray());
        var hex = Convert.ToHexString(Encoding.ASCII.GetBytes(reversed)).ToLowerInvariant();
        var value = HexToBigInteger(hex);
        var pubKey = HexToBigInteger(PubKey);
        var modulus = HexToBigInteger(Modulus);
        var key = System.Numerics.BigInteger.ModPow(value, pubKey, modulus).ToString("x");
        return key.PadLeft(256, '0')[^256..];
    }

    private static System.Numerics.BigInteger HexToBigInteger(string hex)
    {
        var result = System.Numerics.BigInteger.Zero;
        foreach (var c in hex)
        {
            result *= 16;
            result += int.Parse(c.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return result;
    }

    private static string CreateSecretKey(int length)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);
        var builder = new StringBuilder(length);
        foreach (var b in bytes)
        {
            builder.Append(chars[b % chars.Length]);
        }

        return builder.ToString();
    }
}

internal sealed class QqMusicProvider
{
    private const string Provider = "qq";
    private const string Referer = "https://y.qq.com/";

    private readonly HttpClient _client = HttpHelpers.CreateClient();
    private readonly string _defaultCookie = "pgv_pvid=" + RandomNumberGenerator.GetInt32(100000000, 999999999);

    public async Task<IReadOnlyList<SearchItem>> SearchAsync(string keyword, SearchKind type, string? cookie, CancellationToken ct)
    {
        var searchType = type switch
        {
            SearchKind.Song => 0,
            SearchKind.Album => 2,
            SearchKind.Playlist => 3,
            _ => 0
        };

        var body = new Dictionary<string, object?>
        {
            ["req_1"] = new Dictionary<string, object?>
            {
                ["method"] = "DoSearchForQQMusicDesktop",
                ["module"] = "music.search.SearchCgiService",
                ["param"] = new Dictionary<string, object?>
                {
                    ["num_per_page"] = "20",
                    ["page_num"] = "1",
                    ["query"] = keyword,
                    ["search_type"] = searchType
                }
            }
        };

        using var doc = JsonDocument.Parse(await HttpHelpers.PostJsonAsync(_client, "https://u.y.qq.com/cgi-bin/musicu.fcg", body, Referer, GetCookie(cookie), ct));

        var searchBody = doc.RootElement
            .GetProperty("req_1")
            .GetProperty("data")
            .GetProperty("body");

        return type switch
        {
            SearchKind.Song => ParseSongSearch(searchBody),
            SearchKind.Album => ParseAlbumSearch(searchBody),
            SearchKind.Playlist => ParsePlaylistSearch(searchBody),
            _ => []
        };
    }

    public async Task<SongInfo> GetSongAsync(string songId, string? cookie, CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            [songId.All(char.IsDigit) ? "songid" : "songmid"] = songId,
            ["tpl"] = "yqq_song_detail",
            ["format"] = "jsonp",
            ["callback"] = "getOneSongInfoCallback",
            ["g_tk"] = "5381",
            ["jsonpCallback"] = "getOneSongInfoCallback",
            ["loginUin"] = "0",
            ["hostUin"] = "0",
            ["outCharset"] = "utf8",
            ["notice"] = "0",
            ["platform"] = "yqq",
            ["needNewCode"] = "0"
        };

        var raw = await HttpHelpers.PostFormAsync(_client, "https://c.y.qq.com/v8/fcg-bin/fcg_play_single_song.fcg", form, "https://c.y.qq.com/", GetCookie(cookie), ct);
        var json = StripJsonp(raw, "getOneSongInfoCallback");
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Array
            || data.GetArrayLength() == 0)
        {
            throw new ApiException("未查询到歌曲信息。");
        }

        return ParseSong(data[0]);
    }

    public async Task<IReadOnlyList<SongInfo>> GetContainerTracksAsync(string id, SearchKind type, string? cookie, CancellationToken ct)
    {
        if (type == SearchKind.Album)
        {
            var form = new Dictionary<string, string>
            {
                [id.All(char.IsDigit) ? "albumid" : "albummid"] = id
            };
            using var doc = JsonDocument.Parse(await HttpHelpers.PostFormAsync(_client, "https://c.y.qq.com/v8/fcg-bin/fcg_v8_album_info_cp.fcg", form, "https://c.y.qq.com/", GetCookie(cookie), ct));
            var list = doc.RootElement.GetProperty("data").GetProperty("list");
            return list.EnumerateArray().Select(ParseAlbumSong).ToArray();
        }

        var playlistForm = new Dictionary<string, string>
        {
            ["disstid"] = id,
            ["format"] = "json",
            ["outCharset"] = "utf8",
            ["type"] = "1",
            ["json"] = "1",
            ["utf8"] = "1",
            ["onlysong"] = "0",
            ["new_format"] = "1"
        };
        using var playlistDoc = JsonDocument.Parse(await HttpHelpers.PostFormAsync(_client, "https://c.y.qq.com/qzone/fcg-bin/fcg_ucc_getcdinfo_byids_cp.fcg", playlistForm, "https://c.y.qq.com/", GetCookie(cookie), ct));
        var songs = playlistDoc.RootElement.GetProperty("cdlist")[0].GetProperty("songlist");
        return songs.EnumerateArray().Select(ParseSong).ToArray();
    }

    public async Task<RawLyrics> GetLyricsAsync(string songMid, string? cookie, CancellationToken ct)
    {
        var url = "https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?"
            + string.Join('&', new Dictionary<string, string>
            {
                ["songmid"] = songMid,
                ["pcachetime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                ["g_tk"] = "5381",
                ["loginUin"] = "0",
                ["hostUin"] = "0",
                ["format"] = "json",
                ["inCharset"] = "utf8",
                ["outCharset"] = "utf-8",
                ["notice"] = "0",
                ["platform"] = "yqq",
                ["needNewCode"] = "0"
            }.Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));

        using var doc = JsonDocument.Parse(await HttpHelpers.GetStringAsync(_client, url, Referer, GetCookie(cookie), ct));
        if (JsonHelpers.GetString(doc.RootElement, "code") != "0")
        {
            return new RawLyrics("", "", "");
        }

        return new RawLyrics(
            DecodeBase64Text(JsonHelpers.GetString(doc.RootElement, "lyric")),
            DecodeBase64Text(JsonHelpers.GetString(doc.RootElement, "trans")),
            DecodeBase64Text(JsonHelpers.GetString(doc.RootElement, "roma")));
    }

    public async Task<SongLinkResponse> GetSongLinkAsync(string songId, string? cookie, CancellationToken ct)
    {
        var song = await GetSongAsync(songId, cookie, ct);
        var guid = RandomNumberGenerator.GetInt32(100000000, int.MaxValue).ToString(CultureInfo.InvariantCulture);
        var body = new Dictionary<string, object?>
        {
            ["req"] = new Dictionary<string, object?>
            {
                ["method"] = "GetCdnDispatch",
                ["module"] = "CDN.SrfCdnDispatchServer",
                ["param"] = new Dictionary<string, object?>
                {
                    ["guid"] = guid,
                    ["calltype"] = "0",
                    ["userip"] = ""
                }
            },
            ["req_0"] = new Dictionary<string, object?>
            {
                ["method"] = "CgiGetVkey",
                ["module"] = "vkey.GetVkeyServer",
                ["param"] = new Dictionary<string, object?>
                {
                    ["guid"] = guid,
                    ["songmid"] = new[] { song.DisplayId },
                    ["songtype"] = new[] { 1 },
                    ["uin"] = "0",
                    ["loginflag"] = 1,
                    ["platform"] = "20"
                }
            },
            ["comm"] = new Dictionary<string, object?>
            {
                ["uin"] = 0,
                ["format"] = "json",
                ["ct"] = 24,
                ["cv"] = 0
            }
        };

        using var doc = JsonDocument.Parse(await HttpHelpers.PostJsonAsync(_client, "https://u.y.qq.com/cgi-bin/musicu.fcg", body, Referer, GetCookie(cookie), ct));
        var sip = doc.RootElement.GetProperty("req").GetProperty("data").GetProperty("sip");
        var purl = doc.RootElement.GetProperty("req_0").GetProperty("data").GetProperty("midurlinfo")[0].GetProperty("purl").GetString();
        var url = string.IsNullOrWhiteSpace(purl) || sip.GetArrayLength() == 0 ? "" : sip[0].GetString() + purl;
        return new SongLinkResponse(url, "qq-vkey");
    }

    private string GetCookie(string? cookie)
    {
        return string.IsNullOrWhiteSpace(cookie) ? _defaultCookie : cookie;
    }

    private static IReadOnlyList<SearchItem> ParseSongSearch(JsonElement body)
    {
        if (!body.TryGetProperty("song", out var songGroup)
            || !songGroup.TryGetProperty("list", out var songs)
            || songs.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return songs.EnumerateArray()
            .Select(song => new SearchItem(
                Provider,
                "song",
                JsonHelpers.GetString(song, "mid", JsonHelpers.GetString(song, "id")),
                JsonHelpers.GetString(song, "title", JsonHelpers.GetString(song, "name")),
                JsonHelpers.ReadArtists(song, "singer"),
                JsonHelpers.GetNestedString(song, "album", "name"),
                JsonHelpers.GetLong(song, "interval") * 1000,
                BuildCoverUrl(JsonHelpers.GetNestedString(song, "album", "pmid")),
                null,
                null))
            .ToArray();
    }

    private static IReadOnlyList<SearchItem> ParseAlbumSearch(JsonElement body)
    {
        if (!body.TryGetProperty("album", out var albumGroup)
            || !albumGroup.TryGetProperty("list", out var albums)
            || albums.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return albums.EnumerateArray()
            .Select(album => new SearchItem(
                Provider,
                "album",
                JsonHelpers.GetString(album, "albumMID", JsonHelpers.GetString(album, "albumID")),
                JsonHelpers.GetString(album, "albumName"),
                JsonHelpers.ReadArtists(album, "singer_list"),
                null,
                null,
                null,
                JsonHelpers.GetLong(album, "song_count"),
                JsonHelpers.GetString(album, "publicTime")))
            .ToArray();
    }

    private static IReadOnlyList<SearchItem> ParsePlaylistSearch(JsonElement body)
    {
        if (!body.TryGetProperty("songlist", out var playlistGroup)
            || !playlistGroup.TryGetProperty("list", out var playlists)
            || playlists.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return playlists.EnumerateArray()
            .Select(playlist => new SearchItem(
                Provider,
                "playlist",
                JsonHelpers.GetString(playlist, "dissid"),
                JsonHelpers.GetString(playlist, "dissname"),
                [JsonHelpers.GetNestedString(playlist, "creator", "name")],
                null,
                null,
                JsonHelpers.GetString(playlist, "imgurl"),
                JsonHelpers.GetLong(playlist, "song_count"),
                JsonHelpers.GetString(playlist, "introduction")))
            .ToArray();
    }

    private static SongInfo ParseSong(JsonElement song)
    {
        var id = JsonHelpers.GetString(song, "id");
        var mid = JsonHelpers.GetString(song, "mid");
        var albumName = JsonHelpers.GetNestedString(song, "album", "name");
        var albumPmid = JsonHelpers.GetNestedString(song, "album", "pmid");

        return new SongInfo(
            Provider,
            string.IsNullOrWhiteSpace(id) ? mid : id,
            string.IsNullOrWhiteSpace(mid) ? id : mid,
            JsonHelpers.GetString(song, "title", JsonHelpers.GetString(song, "name")),
            JsonHelpers.ReadArtists(song, "singer"),
            albumName,
            JsonHelpers.GetLong(song, "interval") * 1000,
            BuildCoverUrl(albumPmid),
            JsonHelpers.GetString(song, "time_public"));
    }

    private static SongInfo ParseAlbumSong(JsonElement song)
    {
        var id = JsonHelpers.GetString(song, "songid");
        var mid = JsonHelpers.GetString(song, "songmid");
        var singers = JsonHelpers.ReadArtists(song, "singer");

        return new SongInfo(
            Provider,
            string.IsNullOrWhiteSpace(id) ? mid : id,
            string.IsNullOrWhiteSpace(mid) ? id : mid,
            JsonHelpers.GetString(song, "songname"),
            singers,
            "",
            0,
            null,
            null);
    }

    private static string BuildCoverUrl(string albumPmid)
    {
        return string.IsNullOrWhiteSpace(albumPmid)
            ? ""
            : $"https://y.qq.com/music/photo_new/T002R800x800M000{albumPmid}.jpg";
    }

    private static string StripJsonp(string value, string callback)
    {
        value = value.Trim();
        if (!value.StartsWith(callback + "(", StringComparison.Ordinal) || !value.EndsWith(')'))
        {
            return value;
        }

        return value[(callback.Length + 1)..^1];
    }

    private static string DecodeBase64Text(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            return WebUtility.HtmlDecode(decoded);
        }
        catch (FormatException)
        {
            return WebUtility.HtmlDecode(value);
        }
    }
}

internal static class HttpHelpers
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125 Safari/537.36";

    public static HttpClient CreateClient()
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public static async Task<string> PostFormAsync(HttpClient client, string url, Dictionary<string, string> form, string referer, string? cookie, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(form)
        };
        AddHeaders(request, referer, cookie);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public static async Task<string> PostJsonAsync(HttpClient client, string url, Dictionary<string, object?> body, string referer, string? cookie, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        AddHeaders(request, referer, cookie);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public static async Task<string> GetStringAsync(HttpClient client, string url, string referer, string? cookie, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddHeaders(request, referer, cookie);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static void AddHeaders(HttpRequestMessage request, string referer, string? cookie)
    {
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        request.Headers.TryAddWithoutValidation("Referer", referer);
        if (!string.IsNullOrWhiteSpace(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }
    }
}

internal static class JsonHelpers
{
    public static string GetString(JsonElement element, string property, string fallback = "")
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => fallback
        };
    }

    public static string GetNestedString(JsonElement element, string property, string nestedProperty)
    {
        return element.TryGetProperty(property, out var nested) ? GetString(nested, nestedProperty) : "";
    }

    public static long GetLong(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
        {
            return number;
        }

        return long.TryParse(GetString(element, property), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    public static IReadOnlyList<string> ReadArtists(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var artists) || artists.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return artists.EnumerateArray()
            .Select(artist => GetString(artist, "name"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
    }
}
