using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace QuickOcr;

public sealed class HotkeyManager : IDisposable
{
    private const int HotkeyId = 0x514f;
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModAlt = 0x0001;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private readonly Window _window;
    private readonly Func<Task> _onHotkey;
    private HotkeyDefinition _hotkey;
    private HwndSource? _source;
    private IntPtr _handle;
    private bool _registered;

    public HotkeyManager(Window window, HotkeyDefinition hotkey, Func<Task> onHotkey)
    {
        _window = window;
        _hotkey = hotkey;
        _onHotkey = onHotkey;
    }

    public void Register()
    {
        EnsureHook();

        var modifiers = ToNativeModifiers(_hotkey.Modifiers);
        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(_hotkey.Key);
        _registered = RegisterHotKey(_handle, HotkeyId, modifiers, virtualKey);
        if (!_registered)
        {
            System.Windows.MessageBox.Show($"{_hotkey.DisplayText} を登録できませんでした。他のアプリが使用している可能性があります。",
                "Quick OCR",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    public void UpdateHotkey(HotkeyDefinition hotkey)
    {
        if (_registered && _handle != IntPtr.Zero)
        {
            UnregisterHotKey(_handle, HotkeyId);
            _registered = false;
        }

        _hotkey = hotkey;
        Register();
    }

    public void Dispose()
    {
        if (_registered && _handle != IntPtr.Zero)
        {
            UnregisterHotKey(_handle, HotkeyId);
        }

        _source?.RemoveHook(WndProc);
    }

    private void EnsureHook()
    {
        if (_handle != IntPtr.Zero)
        {
            return;
        }

        _handle = new WindowInteropHelper(_window).Handle;
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);
    }

    private static uint ToNativeModifiers(ModifierKeys modifiers)
    {
        uint native = 0;
        if (modifiers.HasFlag(ModifierKeys.Control)) native |= ModControl;
        if (modifiers.HasFlag(ModifierKeys.Alt)) native |= ModAlt;
        if (modifiers.HasFlag(ModifierKeys.Shift)) native |= ModShift;
        if (modifiers.HasFlag(ModifierKeys.Windows)) native |= ModWin;
        return native;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            _ = _onHotkey();
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
