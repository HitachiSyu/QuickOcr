using System.Windows;
using Forms = System.Windows.Forms;

namespace QuickOcr;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "Global\\QuickOcr.SingleInstance";
    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;
    private HotkeyManager? _hotkeyManager;
    private Forms.NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private AppSettings _settings = new();
    private HotkeyDefinition _hotkey = HotkeyDefinition.Default;
    private bool _isCapturing;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        _ownsSingleInstanceMutex = createdNew;
        if (!createdNew)
        {
            System.Windows.MessageBox.Show(
                "Quick OCR \u306f\u3059\u3067\u306b\u8d77\u52d5\u3057\u3066\u3044\u307e\u3059\u3002\n\u30b7\u30e7\u30fc\u30c8\u30ab\u30c3\u30c8\u30ad\u30fc\u3001\u307e\u305f\u306f\u30bf\u30b9\u30af\u30c8\u30ec\u30a4\u306e\u30a2\u30a4\u30b3\u30f3\u304b\u3089\u547c\u3073\u51fa\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
                "Quick OCR",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _settings = AppSettings.Load();
        if (!HotkeyDefinition.TryParse(_settings.Hotkey, out _hotkey))
        {
            _hotkey = HotkeyDefinition.Default;
            _settings.Hotkey = _hotkey.DisplayText;
        }

        if (e.Args.Any(arg => string.Equals(arg, "--settings-preview", StringComparison.OrdinalIgnoreCase)))
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var settingsWindow = new SettingsWindow(_settings);
            MainWindow = settingsWindow;
            settingsWindow.Show();
            return;
        }

        _mainWindow = new MainWindow(StartCaptureAsync, OpenSettings, _hotkey.DisplayText);
        _mainWindow.ShowReady();
        _mainWindow.Show();
        _mainWindow.Hide();

        _hotkeyManager = new HotkeyManager(_mainWindow, _hotkey, StartCaptureAsync);
        _hotkeyManager.Register();
        CreateTrayIcon();

        Dispatcher.BeginInvoke(OpenSettings);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        _hotkeyManager?.Dispose();
        if (_ownsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("\u7bc4\u56f2\u9078\u629e", null, async (_, _) => await StartCaptureAsync());
        menu.Items.Add("\u8a2d\u5b9a", null, (_, _) => OpenSettings());
        menu.Items.Add("\u7d42\u4e86", null, (_, _) => Shutdown());

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = new System.Drawing.Icon(GetResourceStream(new Uri("pack://application:,,,/Assets/QuickOcr.ico")).Stream),
            Text = "Quick OCR",
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => OpenSettings();
    }

    private async Task StartCaptureAsync()
    {
        if (_isCapturing || _mainWindow is null)
        {
            return;
        }

        _isCapturing = true;
        try
        {
            var overlay = new SelectionOverlay();
            var selected = overlay.ShowDialog() == true;
            if (!selected || overlay.SelectedBounds.Width <= 2 || overlay.SelectedBounds.Height <= 2)
            {
                return;
            }

            await Task.Delay(80);
            _mainWindow.ShowLoading();
            using var bitmap = ScreenCapture.Capture(overlay.SelectedBounds);
            var result = await WindowsOcrService.RecognizeAsync(bitmap, _settings.OcrLanguages);
            _mainWindow.ShowResult(result.Text, result.LanguageTag, result.Elapsed);
        }
        catch (Exception ex)
        {
            _mainWindow.ShowResult(ex.Message, "OCR error", TimeSpan.Zero);
        }
        finally
        {
            _isCapturing = false;
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void OpenSettings()
    {
        if (_mainWindow is null || _hotkeyManager is null)
        {
            return;
        }

        var window = new SettingsWindow(_settings) { Owner = _mainWindow };
        if (window.ShowDialog() != true)
        {
            return;
        }

        _settings.Hotkey = window.Settings.Hotkey;
        _settings.OcrLanguage = window.Settings.OcrLanguage;
        _settings.OcrLanguages = new List<string>(window.Settings.OcrLanguages);
        _settings.NormalizeLanguages();
        _settings.Save();

        if (HotkeyDefinition.TryParse(_settings.Hotkey, out _hotkey))
        {
            _hotkeyManager.UpdateHotkey(_hotkey);
            _mainWindow.UpdateHotkeyText(_hotkey.DisplayText);
        }
    }
}
