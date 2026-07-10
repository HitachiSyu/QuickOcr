using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickOcr;

public partial class SettingsWindow : Window
{
    private HotkeyDefinition _hotkey;
    private bool _updatingLanguages;

    public AppSettings Settings { get; }

    public SettingsWindow(AppSettings settings)
    {
        Settings = new AppSettings
        {
            Hotkey = settings.Hotkey,
            OcrLanguage = settings.OcrLanguage,
            OcrLanguages = new List<string>(settings.OcrLanguages)
        };
        Settings.NormalizeLanguages();

        HotkeyDefinition.TryParse(Settings.Hotkey, out _hotkey);
        InitializeComponent();
        HotkeyTextBox.Text = _hotkey.DisplayText;
        SelectLanguages(Settings.OcrLanguages);
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HotkeyDefinition.IsModifierKey(key))
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None)
        {
            return;
        }

        _hotkey = new HotkeyDefinition(modifiers, key);
        HotkeyTextBox.Text = _hotkey.DisplayText;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Settings.Hotkey = _hotkey.DisplayText;
        Settings.OcrLanguages = GetSelectedLanguages();
        Settings.NormalizeLanguages();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SelectLanguages(IReadOnlyCollection<string> languages)
    {
        _updatingLanguages = true;
        JapaneseCheckBox.IsChecked = languages.Contains("Japanese", StringComparer.OrdinalIgnoreCase);
        EnglishCheckBox.IsChecked = languages.Contains("English", StringComparer.OrdinalIgnoreCase);
        ChineseCheckBox.IsChecked = languages.Contains("Chinese", StringComparer.OrdinalIgnoreCase);
        _updatingLanguages = false;

        EnsureAtLeastOneLanguage();
    }

    private List<string> GetSelectedLanguages()
    {
        var languages = new List<string>();
        if (JapaneseCheckBox.IsChecked == true) languages.Add("Japanese");
        if (EnglishCheckBox.IsChecked == true) languages.Add("English");
        if (ChineseCheckBox.IsChecked == true) languages.Add("Chinese");
        return languages;
    }

    private void LanguageCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_updatingLanguages)
        {
            EnsureAtLeastOneLanguage();
        }
    }

    private void EnsureAtLeastOneLanguage()
    {
        if (JapaneseCheckBox.IsChecked == true
            || EnglishCheckBox.IsChecked == true
            || ChineseCheckBox.IsChecked == true)
        {
            return;
        }

        JapaneseCheckBox.IsChecked = true;
    }
}
