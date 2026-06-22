using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicLyrics.Api;

public static class LyricsFormatter
{
    private static readonly Regex LrcTimestampRegex = new(@"\[(\d+):(\d{1,2})(?:[.:](\d{1,3}))?]", RegexOptions.Compiled);
    private static readonly Regex SrtTimestampRegex = new(@"^\s*(\d{1,2}):(\d{2}):(\d{2})[,.](\d{1,3})\s*$", RegexOptions.Compiled);

    public static string Format(RawLyrics raw, string format, long durationMs, bool includeTranslation, bool includeTransliteration)
    {
        var lines = BuildLines(raw, includeTranslation, includeTransliteration);
        if (lines.Count == 0)
        {
            return raw.Lyric;
        }

        return format.Trim().ToLowerInvariant() switch
        {
            "srt" => ToSrt(lines, durationMs),
            _ => ToLrc(lines)
        };
    }

    public static ConvertLyricsResponse Convert(ConvertLyricsRequest request)
    {
        var from = request.From.Trim().ToLowerInvariant();
        var to = request.To.Trim().ToLowerInvariant();

        if (from == to)
        {
            return new ConvertLyricsResponse(request.Input);
        }

        if (from == "lrc" && to == "srt")
        {
            return new ConvertLyricsResponse(ToSrt(ParseLrc(request.Input), request.DurationMs));
        }

        if (from == "srt" && to == "lrc")
        {
            return new ConvertLyricsResponse(SrtToLrc(request.Input));
        }

        throw new ApiException("仅支持 LRC 与 SRT 互转。");
    }

    private static List<LyricLine> BuildLines(RawLyrics raw, bool includeTranslation, bool includeTransliteration)
    {
        var origin = ParseLrc(raw.Lyric);
        var translation = includeTranslation ? ParseLrc(raw.Translation) : [];
        var transliteration = includeTransliteration ? ParseLrc(raw.Transliteration) : [];

        if (origin.Count == 0)
        {
            return translation.Count > 0 ? translation : transliteration;
        }

        var transByTime = translation
            .GroupBy(x => x.TimeMs)
            .ToDictionary(x => x.Key, x => string.Join(" / ", x.Select(y => y.Text).Where(y => !string.IsNullOrWhiteSpace(y))));
        var romaByTime = transliteration
            .GroupBy(x => x.TimeMs)
            .ToDictionary(x => x.Key, x => string.Join(" / ", x.Select(y => y.Text).Where(y => !string.IsNullOrWhiteSpace(y))));

        var result = new List<LyricLine>();
        foreach (var line in origin)
        {
            var parts = new List<string> { line.Text };

            if (transByTime.TryGetValue(line.TimeMs, out var trans) && !string.IsNullOrWhiteSpace(trans))
            {
                parts.Add(trans);
            }

            if (romaByTime.TryGetValue(line.TimeMs, out var roma) && !string.IsNullOrWhiteSpace(roma))
            {
                parts.Add(roma);
            }

            result.Add(line with { Text = string.Join(Environment.NewLine, parts.Where(x => !string.IsNullOrWhiteSpace(x))) });
        }

        return result;
    }

    private static List<LyricLine> ParseLrc(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var result = new List<LyricLine>();
        var rows = input.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        foreach (var row in rows)
        {
            var line = row.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var matches = LrcTimestampRegex.Matches(line);
            if (matches.Count == 0)
            {
                continue;
            }

            var content = LrcTimestampRegex.Replace(line, "").Trim();
            foreach (Match match in matches)
            {
                result.Add(new LyricLine(ParseLrcTime(match), content));
            }
        }

        return result.OrderBy(x => x.TimeMs).ToList();
    }

    private static string ToLrc(IReadOnlyList<LyricLine> lines)
    {
        return string.Join(Environment.NewLine, lines.Select(line =>
        {
            var parts = line.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            return string.Join(Environment.NewLine, parts.Select(part => $"{FormatLrcTime(line.TimeMs)}{part}"));
        }));
    }

    private static string ToSrt(IReadOnlyList<LyricLine> lines, long durationMs)
    {
        if (lines.Count == 0)
        {
            return "";
        }

        var sb = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            var current = lines[i];
            var nextStart = i + 1 < lines.Count ? lines[i + 1].TimeMs : durationMs;
            if (nextStart <= current.TimeMs)
            {
                nextStart = current.TimeMs + 3000;
            }

            sb.Append(i + 1)
                .AppendLine()
                .Append(FormatSrtTime(current.TimeMs))
                .Append(" --> ")
                .Append(FormatSrtTime(nextStart))
                .AppendLine()
                .AppendLine(current.Text)
                .AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string SrtToLrc(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        var blocks = input.Replace("\r\n", "\n").Replace('\r', '\n').Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<LyricLine>();

        foreach (var block in blocks)
        {
            var rows = block.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            var rangeIndex = rows.FindIndex(x => x.Contains("-->", StringComparison.Ordinal));
            if (rangeIndex < 0 || rangeIndex + 1 >= rows.Count)
            {
                continue;
            }

            var range = rows[rangeIndex].Split("-->", StringSplitOptions.TrimEntries);
            if (range.Length != 2)
            {
                continue;
            }

            var content = string.Join(" ", rows.Skip(rangeIndex + 1));
            lines.Add(new LyricLine(ParseSrtTime(range[0]), content));
        }

        return ToLrc(lines.OrderBy(x => x.TimeMs).ToArray());
    }

    private static long ParseLrcTime(Match match)
    {
        var minute = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var second = long.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var msText = match.Groups[3].Success ? match.Groups[3].Value : "0";
        var ms = msText.Length switch
        {
            1 => long.Parse(msText, CultureInfo.InvariantCulture) * 100,
            2 => long.Parse(msText, CultureInfo.InvariantCulture) * 10,
            _ => long.Parse(msText[..Math.Min(msText.Length, 3)], CultureInfo.InvariantCulture)
        };

        return ((minute * 60) + second) * 1000 + ms;
    }

    private static long ParseSrtTime(string value)
    {
        var match = SrtTimestampRegex.Match(value);
        if (!match.Success)
        {
            throw new ApiException($"非法 SRT 时间戳：{value}");
        }

        var hour = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minute = long.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var second = long.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        var msText = match.Groups[4].Value;
        var ms = msText.Length switch
        {
            1 => long.Parse(msText, CultureInfo.InvariantCulture) * 100,
            2 => long.Parse(msText, CultureInfo.InvariantCulture) * 10,
            _ => long.Parse(msText[..Math.Min(msText.Length, 3)], CultureInfo.InvariantCulture)
        };

        return (((hour * 60 + minute) * 60) + second) * 1000 + ms;
    }

    private static string FormatLrcTime(long ms)
    {
        var totalSeconds = ms / 1000;
        var minute = totalSeconds / 60;
        var second = totalSeconds % 60;
        var millisecond = ms % 1000;
        return $"[{minute:00}:{second:00}.{millisecond:000}]";
    }

    private static string FormatSrtTime(long ms)
    {
        var totalSeconds = ms / 1000;
        var hour = totalSeconds / 3600;
        var minute = totalSeconds % 3600 / 60;
        var second = totalSeconds % 60;
        var millisecond = ms % 1000;
        return $"{hour:00}:{minute:00}:{second:00},{millisecond:000}";
    }

    private sealed record LyricLine(long TimeMs, string Text);
}
