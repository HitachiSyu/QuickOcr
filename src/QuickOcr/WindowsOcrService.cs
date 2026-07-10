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
        return await RecognizeAsync(source, LegacyLanguageToList(languagePreference));
    }

    public static async Task<RecognizedText> RecognizeAsync(DrawingBitmap source, IReadOnlyList<string> languagePreferences)
    {
        using var bitmap = NormalizeImageSize(source);
        var stopwatch = Stopwatch.StartNew();
        var selectedLanguages = NormalizeLanguagePreferences(languagePreferences);

        await using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Png);
        memory.Position = 0;

        using var randomAccessStream = memory.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        var primaryEngine = CreatePrimaryEngine(selectedLanguages);
        var primaryResult = await primaryEngine.RecognizeAsync(softwareBitmap);
        var text = BuildTextWithLineBreaks(primaryResult);
        var languageTag = primaryEngine.RecognizerLanguage.LanguageTag;

        if (ShouldUseEnglishAssist(selectedLanguages, languageTag))
        {
            if (!TryCreateEngine("English", out var englishEngine))
            {
                throw new InvalidOperationException("\u82f1\u6570\u5b57/URL \u88dc\u6b63\u306b\u306f\u3001\u82f1\u8a9e\u306e Windows OCR \u8a00\u8a9e\u30d1\u30c3\u30af\u304c\u5fc5\u8981\u3067\u3059\u3002Windows \u306e\u8a2d\u5b9a\u304b\u3089\u82f1\u8a9e\u306e OCR \u8a00\u8a9e\u30b5\u30dd\u30fc\u30c8\u3092\u8ffd\u52a0\u3057\u3066\u304f\u3060\u3055\u3044\u3002");
            }

            if (string.Equals(englishEngine.RecognizerLanguage.LanguageTag, languageTag, StringComparison.OrdinalIgnoreCase))
            {
                stopwatch.Stop();
                return new RecognizedText(text, languageTag, stopwatch.Elapsed);
            }

            var englishResult = await englishEngine.RecognizeAsync(softwareBitmap);
            var englishText = BuildTextWithLineBreaks(englishResult);
            text = MergeWithEnglishForAsciiLines(primaryResult, englishResult);
            if (string.IsNullOrWhiteSpace(text))
            {
                text = englishText;
            }
            else if (ShouldPreferWholeEnglishText(text, englishText))
            {
                text = englishText;
            }

            languageTag = $"{languageTag}+{englishEngine.RecognizerLanguage.LanguageTag}";
        }

        if (await ShouldUseChineseFallbackAsync(selectedLanguages, softwareBitmap, text) is { } chineseFallback)
        {
            text = chineseFallback.Text;
            languageTag = $"{languageTag}+{chineseFallback.LanguageTag}";
        }

        stopwatch.Stop();
        return new RecognizedText(text, languageTag, stopwatch.Elapsed);
    }

    private static List<string> LegacyLanguageToList(string languagePreference)
    {
        return languagePreference switch
        {
            "Japanese" => ["Japanese"],
            "English" => ["English"],
            "Chinese" => ["Chinese"],
            _ => ["Japanese", "English", "Chinese"]
        };
    }

    private static List<string> NormalizeLanguagePreferences(IReadOnlyList<string> languagePreferences)
    {
        var languages = languagePreferences
            .Select(language => language?.Trim())
            .Where(language => language is "Japanese" or "English" or "Chinese")
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(LanguagePriority)
            .ToList();

        return languages.Count == 0 ? ["Japanese", "English", "Chinese"] : languages;
    }

    private static int LanguagePriority(string language)
    {
        return language switch
        {
            "Japanese" => 0,
            "English" => 1,
            "Chinese" => 2,
            _ => 99
        };
    }

    private static OcrEngine CreateEngine(string languagePreference)
    {
        if (TryCreateEngine(languagePreference, out var engine))
        {
            return engine;
        }

        throw new InvalidOperationException("\u5229\u7528\u53ef\u80fd\u306a Windows OCR \u8a00\u8a9e\u30d1\u30c3\u30af\u304c\u898b\u3064\u304b\u308a\u307e\u305b\u3093\u3002Windows \u306e\u8a2d\u5b9a\u304b\u3089\u65e5\u672c\u8a9e\u3001\u82f1\u8a9e\u3001\u307e\u305f\u306f\u4e2d\u56fd\u8a9e\u306e OCR \u8a00\u8a9e\u30b5\u30dd\u30fc\u30c8\u3092\u8ffd\u52a0\u3057\u3066\u304f\u3060\u3055\u3044\u3002");
    }

    private static OcrEngine CreatePrimaryEngine(IReadOnlyList<string> languagePreferences)
    {
        foreach (var language in languagePreferences)
        {
            if (TryCreateEngine(language, out var engine))
            {
                return engine;
            }
        }

        throw new InvalidOperationException("\u9078\u629e\u3055\u308c\u305f Windows OCR \u8a00\u8a9e\u30d1\u30c3\u30af\u304c\u898b\u3064\u304b\u308a\u307e\u305b\u3093\u3002Windows \u306e\u8a2d\u5b9a\u304b\u3089 OCR \u8a00\u8a9e\u30b5\u30dd\u30fc\u30c8\u3092\u8ffd\u52a0\u3057\u3066\u304f\u3060\u3055\u3044\u3002");
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

    private static bool ShouldUseEnglishAssist(IReadOnlyCollection<string> languagePreferences, string primaryLanguageTag)
    {
        if (!languagePreferences.Contains("English", StringComparer.OrdinalIgnoreCase)
            || primaryLanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return primaryLanguageTag.StartsWith("ja", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<RecognizedText?> ShouldUseChineseFallbackAsync(
        IReadOnlyCollection<string> languagePreferences,
        SoftwareBitmap softwareBitmap,
        string currentText)
    {
        if (!languagePreferences.Contains("Chinese", StringComparer.OrdinalIgnoreCase)
            || languagePreferences.Count <= 1
            || HasUsableText(currentText)
            || !TryCreateEngine("Chinese", out var chineseEngine))
        {
            return null;
        }

        var chineseResult = await chineseEngine.RecognizeAsync(softwareBitmap);
        var chineseText = BuildTextWithLineBreaks(chineseResult);
        return HasUsableText(chineseText)
            ? new RecognizedText(chineseText, chineseEngine.RecognizerLanguage.LanguageTag, TimeSpan.Zero)
            : null;
    }

    private static bool HasUsableText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var meaningful = text.Count(ch => char.IsLetterOrDigit(ch) || IsCjkUnifiedIdeograph(ch) || IsJapaneseKana(ch));
        return meaningful >= 3;
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

    private static bool ShouldPreferWholeEnglishText(string currentText, string englishText)
    {
        if (string.IsNullOrWhiteSpace(englishText)
            || HasJapaneseText(currentText))
        {
            return false;
        }

        var compactEnglish = englishText.ReplaceLineEndings(" ");
        var nonWhiteEnglish = compactEnglish.Count(ch => !char.IsWhiteSpace(ch));
        if (nonWhiteEnglish == 0)
        {
            return false;
        }

        var asciiEnglish = compactEnglish.Count(ch => ch <= 0x7f && !char.IsWhiteSpace(ch));
        if (asciiEnglish < nonWhiteEnglish * 0.9 || !LooksLikeUrlOrTechnicalText(compactEnglish))
        {
            return false;
        }

        var englishAsciiLetters = compactEnglish.Count(IsAsciiLetter);
        var currentAsciiLetters = currentText.Count(IsAsciiLetter);
        if (englishAsciiLetters >= currentAsciiLetters + 4)
        {
            return true;
        }

        return ScoreAsciiLine(compactEnglish) >= ScoreAsciiLine(currentText.ReplaceLineEndings(" "));
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
            .Select(line =>
            {
                var position = GetLinePosition(line);
                return new
                {
                    position.Top,
                    position.Left,
                    Text = BuildLineText(line.Words.Select(word => word.Text))
                };
            })
            .Where(line => !string.IsNullOrWhiteSpace(line.Text))
            .OrderBy(line => line.Top)
            .ThenBy(line => line.Left)
            .Select(line => line.Text)
            .ToList();
    }

    private static (double Top, double Left) GetLinePosition(Windows.Media.Ocr.OcrLine line)
    {
        var words = line.Words.ToList();
        if (words.Count == 0)
        {
            return (double.MaxValue, double.MaxValue);
        }

        return (
            words.Min(word => word.BoundingRect.Top),
            words.Min(word => word.BoundingRect.Left));
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

    private static bool IsAsciiLetter(char value)
    {
        return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static bool IsAsciiUrlNeighbor(char value)
    {
        return value <= 0x7f && (char.IsLetterOrDigit(value) || value is '/' or '\\' or '.' or '_' or '-' or ':' or '@');
    }

    private static bool IsCjkUnifiedIdeograph(char value)
    {
        return value is >= '\u4e00' and <= '\u9fff';
    }

    private static bool IsJapaneseKana(char value)
    {
        return value is >= '\u3040' and <= '\u30ff';
    }

    private static bool HasJapaneseText(string text)
    {
        return text.Count(IsJapaneseKana) >= 3;
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

        if (Math.Abs(scale - 1.0) < 0.01 && source.Width < 1600 && source.Height < 1200)
        {
            scale = 2.0;
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
