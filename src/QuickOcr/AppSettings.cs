using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace QuickOcr;

public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string Hotkey { get; set; } = "Ctrl+Shift+O";
    public string OcrLanguage { get; set; } = "Auto";

    public static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "quickocr.settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
    }
}

public sealed record HotkeyDefinition(ModifierKeys Modifiers, Key Key)
{
    public string DisplayText
    {
        get
        {
            var parts = new List<string>();
            if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
            parts.Add(Key.ToString().ToUpperInvariant());
            return string.Join("+", parts);
        }
    }

    public static HotkeyDefinition Default => new(ModifierKeys.Control | ModifierKeys.Shift, Key.O);

    public static bool TryParse(string value, out HotkeyDefinition hotkey)
    {
        hotkey = Default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var modifiers = ModifierKeys.None;
        Key? key = null;
        foreach (var rawPart in value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var part = rawPart.ToUpperInvariant();
            switch (part)
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= ModifierKeys.Control;
                    break;
                case "ALT":
                    modifiers |= ModifierKeys.Alt;
                    break;
                case "SHIFT":
                    modifiers |= ModifierKeys.Shift;
                    break;
                case "WIN":
                case "WINDOWS":
                    modifiers |= ModifierKeys.Windows;
                    break;
                default:
                    if (Enum.TryParse<Key>(rawPart, true, out var parsedKey))
                    {
                        key = parsedKey;
                    }
                    break;
            }
        }

        if (modifiers == ModifierKeys.None || key is null || IsModifierKey(key.Value))
        {
            return false;
        }

        hotkey = new HotkeyDefinition(modifiers, key.Value);
        return true;
    }

    public static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin
            or Key.System;
    }
}
