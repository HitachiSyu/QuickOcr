using System.Windows;
using System.Windows.Input;

namespace QuickOcr;

public partial class MainWindow : Window
{
    private readonly Func<Task> _retake;
    private readonly Action _openSettings;

    public MainWindow(Func<Task> retake, Action openSettings, string hotkeyText)
    {
        _retake = retake;
        _openSettings = openSettings;
        InitializeComponent();
        UpdateHotkeyText(hotkeyText);
    }

    public void UpdateHotkeyText(string hotkeyText)
    {
        HotkeyTextBlock.Text = hotkeyText;
    }

    public void ShowReady()
    {
        ResultTextBox.Text = "\u30b7\u30e7\u30fc\u30c8\u30ab\u30c3\u30c8\u30ad\u30fc\u3001\u307e\u305f\u306f\u30bf\u30b9\u30af\u30c8\u30ec\u30a4\u306e\u300c\u7bc4\u56f2\u9078\u629e\u300d\u304b\u3089 OCR \u3092\u958b\u59cb\u3067\u304d\u307e\u3059\u3002";
        StatusTextBlock.Text = "Ready";
        CopyButton.Content = "\u30b3\u30d4\u30fc";
    }

    public void ShowLoading()
    {
        ResultTextBox.Text = "認識中...";
        StatusTextBlock.Text = "Windows OCR";
        CopyButton.Content = "コピー";
        Show();
        Activate();
    }

    public void ShowResult(string text, string languageTag, TimeSpan elapsed)
    {
        ResultTextBox.Text = text;
        StatusTextBlock.Text = $"{languageTag}  |  {text.Length} chars  |  {elapsed.TotalMilliseconds:0} ms";
        CopyButton.Content = "コピー";
        Show();
        Activate();
        ResultTextBox.Focus();
        ResultTextBox.SelectAll();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Clipboard.SetText(ResultTextBox.Text);
        CopyButton.Content = "コピーしました";
    }

    private async void RetakeButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        await _retake();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _openSettings();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
