using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickOcr;

public partial class SettingsWindow : Window
{
    private HotkeyDefinition _hotkey;

    public AppSettings Settings { get; }

    public SettingsWindow(AppSettings settings)
    {
        Settings = new AppSettings
        {
            Hotkey = settings.Hotkey,
            OcrLanguage = settings.OcrLanguage
        };

        HotkeyDefinition.TryParse(Settings.Hotkey, out _hotkey);
        InitializeComponent();
        HotkeyTextBox.Text = _hotkey.DisplayText;
        SelectLanguage(Settings.OcrLanguage);
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
        Settings.OcrLanguage = ((ComboBoxItem)LanguageComboBox.SelectedItem).Tag?.ToString() ?? "Auto";
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SelectLanguage(string language)
    {
        foreach (ComboBoxItem item in LanguageComboBox.Items)
        {
            if (string.Equals(item.Tag?.ToString(), language, StringComparison.OrdinalIgnoreCase))
            {
                LanguageComboBox.SelectedItem = item;
                return;
            }
        }

        LanguageComboBox.SelectedIndex = 0;
    }
}
