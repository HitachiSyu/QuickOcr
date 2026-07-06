using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using DrawingBitmap = System.Drawing.Bitmap;

namespace QuickOcr;

public sealed record RecognizedText(string Text, string LanguageTag, TimeSpan Elapsed);

public static class WindowsOcrService
{
    public static async Task<RecognizedText> RecognizeAsync(DrawingBitmap source, string languagePreference)
    {
        using var bitmap = NormalizeImageSize(source);
        var stopwatch = Stopwatch.StartNew();

        await using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Png);
        memory.Position = 0;

        using var randomAccessStream = memory.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        var primaryEngine = CreateEngine(languagePreference);
        var primaryResult = await primaryEngine.RecognizeAsync(softwareBitmap);
        var text = BuildTextWithLineBreaks(primaryResult);
        var languageTag = primaryEngine.RecognizerLanguage.LanguageTag;

        if (ShouldUseEnglishAssist(languagePreference, languageTag))
        {
            if (!TryCreateEngine("English", out var englishEngine))
            {
                throw new InvalidOperationException("\u81ea\u52d5\u30e2\u30fc\u30c9\u3068\u65e5\u672c\u8a9e\u30e2\u30fc\u30c9\u306e\u82f1\u6570\u5b57/URL \u88dc\u6b63\u306b\u306f\u3001\u82f1\u8a9e\u306e Windows OCR \u8a00\u8a9e\u30d1\u30c3\u30af\u304c\u5fc5\u8981\u3067\u3059\u3002Windows \u306e\u8a2d\u5b9a\u304b\u3089\u82f1\u8a9e\u306e OCR \u8a00\u8a9e\u30b5\u30dd\u30fc\u30c8\u3092\u8ffd\u52a0\u3057\u3066\u304f\u3060\u3055\u3044\u3002");
            }

            if (string.Equals(englishEngine.RecognizerLanguage.LanguageTag, languageTag, StringComparison.OrdinalIgnoreCase))
            {
                stopwatch.Stop();
                return new RecognizedText(text, languageTag, stopwatch.Elapsed);
            }

            var englishResult = await englishEngine.RecognizeAsync(softwareBitmap);
            text = MergeWithEnglishForAsciiLines(primaryResult, englishResult);
            if (string.IsNullOrWhiteSpace(text))
            {
                text = BuildTextWithLineBreaks(englishResult);
            }

            languageTag = $"{languageTag}+{englishEngine.RecognizerLanguage.LanguageTag}";
        }

        stopwatch.Stop();
        return new RecognizedText(text, languageTag, stopwatch.Elapsed);
    }

    private static OcrEngine CreateEngine(string languagePreference)
    {
        if (TryCreateEngine(languagePreference, out var engine))
        {
            return engine;
        }

        throw new InvalidOperationException("\u5229\u7528\u53ef\u80fd\u306a Windows OCR \u8a00\u8a9e\u30d1\u30c3\u30af\u304c\u898b\u3064\u304b\u308a\u307e\u305b\u3093\u3002Windows \u306e\u8a2d\u5b9a\u304b\u3089\u65e5\u672c\u8a9e\u3001\u82f1\u8a9e\u3001\u307e\u305f\u306f\u4e2d\u56fd\u8a9e\u306e OCR \u8a00\u8a9e\u30b5\u30dd\u30fc\u30c8\u3092\u8ffd\u52a0\u3057\u3066\u304f\u3060\u3055\u3044\u3002");
    }

    private static bool TryCreateEngine(string languagePreference, out OcrEngine engine)
    {
        engine = null!;
        var available = OcrEngine.AvailableRecognizerLanguages.ToList();
        if (available.Count == 0)
        {
            return false;
        }

        var language = SelectLanguage(available, languagePreference);
        if (language is null)
        {
            return false;
        }

        var created = OcrEngine.TryCreateFromLanguage(language);
        if (created is null)
        {
            return false;
        }

        engine = created;
        return true;
    }

    private static Language? SelectLanguage(IReadOnlyList<Language> available, string languagePreference)
    {
        var preferredPrefixes = languagePreference switch
        {
            "Japanese" => new[] { "ja" },
            "English" => new[] { "en" },
            "Chinese" => new[] { "zh" },
            _ => new[] { "ja", "en", "zh" }
        };

        foreach (var prefix in preferredPrefixes)
        {
            var language = available.FirstOrDefault(item =>
                item.LanguageTag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (language is not null)
            {
                return language;
            }
        }

        return languagePreference == "Auto" ? available[0] : null;
    }

    private static bool ShouldUseEnglishAssist(string languagePreference, string primaryLanguageTag)
    {
        if (languagePreference is "English" or "Chinese")
        {
            return false;
        }

        return primaryLanguageTag.StartsWith("ja", StringComparison.OrdinalIgnoreCase)
            || string.Equals(languagePreference, "Auto", StringComparison.OrdinalIgnoreCase);
    }

    private static string MergeWithEnglishForAsciiLines(Windows.Media.Ocr.OcrResult primary, Windows.Media.Ocr.OcrResult english)
    {
        var primaryLines = BuildLines(primary);
        var englishLines = BuildLines(english);
        var count = Math.Max(primaryLines.Count, englishLines.Count);
        var merged = new List<string>();

        for (var index = 0; index < count; index++)
        {
            var primaryLine = index < primaryLines.Count ? primaryLines[index] : string.Empty;
            var englishLine = index < englishLines.Count ? englishLines[index] : string.Empty;

            if (ShouldPreferEnglishLine(primaryLine, englishLine))
            {
                merged.Add(CleanAsciiLine(englishLine));
            }
            else if (!string.IsNullOrWhiteSpace(primaryLine))
            {
                merged.Add(primaryLine);
            }
            else if (!string.IsNullOrWhiteSpace(englishLine))
            {
                merged.Add(CleanAsciiLine(englishLine));
            }
        }

        return string.Join(Environment.NewLine, merged.Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static bool ShouldPreferEnglishLine(string primaryLine, string englishLine)
    {
        if (string.IsNullOrWhiteSpace(englishLine))
        {
            return false;
        }

        var cleanedEnglish = CleanAsciiLine(englishLine);
        if (LooksLikeUrlOrTechnicalText(cleanedEnglish))
        {
            return true;
        }

        if (!LooksLikeUrlOrTechnicalText(primaryLine))
        {
            return false;
        }

        return ScoreAsciiLine(cleanedEnglish) >= ScoreAsciiLine(primaryLine);
    }

    private static bool LooksLikeUrlOrTechnicalText(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var lower = line.ToLowerInvariant();
        if (lower.Contains("http")
            || lower.Contains("www.")
            || lower.Contains(".com")
            || lower.Contains(".jp")
            || lower.Contains(".net")
            || lower.Contains(".org"))
        {
            return true;
        }

        var ascii = line.Count(ch => ch <= 0x7f);
        var technical = line.Count(ch => ch is '/' or '\\' or '.' or '_' or '-' or ':' or '@' or '?' or '&' or '=' or '#');
        var digits = line.Count(char.IsDigit);
        return ascii >= line.Length * 0.65 && (technical >= 2 || digits >= 4);
    }

    private static int ScoreAsciiLine(string line)
    {
        var score = 0;
        foreach (var ch in line)
        {
            if (ch <= 0x7f && !char.IsWhiteSpace(ch)) score += 2;
            if (char.IsLetterOrDigit(ch)) score += 1;
            if (ch is '/' or '.' or '_' or '-' or ':' or '@') score += 2;
            if (ch > 0x7f) score -= 4;
        }

        return score;
    }

    private static string BuildTextWithLineBreaks(Windows.Media.Ocr.OcrResult result)
    {
        var lines = BuildLines(result);
        return lines.Count == 0 ? result.Text.Trim() : string.Join(Environment.NewLine, lines);
    }

    private static List<string> BuildLines(Windows.Media.Ocr.OcrResult result)
    {
        return result.Lines
            .Select(line => BuildLineText(line.Words.Select(word => word.Text)))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static string BuildLineText(IEnumerable<string> words)
    {
        var result = new List<string>();
        foreach (var word in words.Where(word => !string.IsNullOrWhiteSpace(word)))
        {
            if (result.Count > 0 && NeedsSpace(result[^1], word))
            {
                result.Add(" ");
            }

            result.Add(word);
        }

        return string.Concat(result).Trim();
    }

    private static string CleanAsciiLine(string line)
    {
        var result = new List<char>();
        foreach (var ch in line.Trim())
        {
            if (result.Count > 0
                && char.IsWhiteSpace(ch)
                && IsAsciiUrlNeighbor(result[^1]))
            {
                continue;
            }

            result.Add(ch);
        }

        return string.Concat(result).Replace("\uff0e", ".").Replace("\u3002", ".");
    }

    private static bool NeedsSpace(string left, string right)
    {
        return IsAsciiLetterOrDigit(left[^1]) && IsAsciiLetterOrDigit(right[0]);
    }

    private static bool IsAsciiLetterOrDigit(char value)
    {
        return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9';
    }

    private static bool IsAsciiUrlNeighbor(char value)
    {
        return value <= 0x7f && (char.IsLetterOrDigit(value) || value is '/' or '\\' or '.' or '_' or '-' or ':' or '@');
    }

    private static DrawingBitmap NormalizeImageSize(DrawingBitmap source)
    {
        var maxDimension = OcrEngine.MaxImageDimension;
        var scale = 1.0;

        if (source.Height < 96)
        {
            scale = Math.Max(scale, 96.0 / source.Height);
        }

        if (source.Width < 480)
        {
            scale = Math.Max(scale, 480.0 / source.Width);
        }

        if (source.Width * scale > maxDimension || source.Height * scale > maxDimension)
        {
            scale = Math.Min((double)maxDimension / source.Width, (double)maxDimension / source.Height);
        }

        scale = Math.Min(scale, 4.0);
        if (Math.Abs(scale - 1.0) < 0.01)
        {
            return new DrawingBitmap(source);
        }

        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));

        var resized = new DrawingBitmap(width, height);
        using var graphics = System.Drawing.Graphics.FromImage(resized);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(source, 0, 0, width, height);
        return resized;
    }
}
