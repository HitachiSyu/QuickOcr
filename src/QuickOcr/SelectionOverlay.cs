using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using WpfPoint = System.Windows.Point;
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace QuickOcr;

public sealed class SelectionOverlay : Window
{
    private readonly Canvas _canvas = new();
    private readonly WpfRectangle _selection = new();
    private WpfPoint _start;
    private bool _dragging;

    public System.Drawing.Rectangle SelectedBounds { get; private set; }

    public SelectionOverlay()
    {
        var bounds = GetVirtualScreenBounds();

        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(74, 0, 0, 0));
        Topmost = true;
        ShowInTaskbar = false;
        Cursor = System.Windows.Input.Cursors.Cross;

        _selection.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));
        _selection.StrokeThickness = 2;
        _selection.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(42, 0, 120, 215));
        _selection.Visibility = Visibility.Collapsed;
        _canvas.Children.Add(_selection);
        Content = _canvas;

        KeyDown += OnKeyDown;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    private static Rect GetVirtualScreenBounds()
    {
        var left = Forms.Screen.AllScreens.Min(screen => screen.Bounds.Left);
        var top = Forms.Screen.AllScreens.Min(screen => screen.Bounds.Top);
        var right = Forms.Screen.AllScreens.Max(screen => screen.Bounds.Right);
        var bottom = Forms.Screen.AllScreens.Max(screen => screen.Bounds.Bottom);
        return new Rect(left, top, right - left, bottom - top);
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _start = e.GetPosition(_canvas);
        _selection.Visibility = Visibility.Visible;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var current = e.GetPosition(_canvas);
        var x = Math.Min(_start.X, current.X);
        var y = Math.Min(_start.Y, current.Y);
        var width = Math.Abs(current.X - _start.X);
        var height = Math.Abs(current.Y - _start.Y);

        Canvas.SetLeft(_selection, x);
        Canvas.SetTop(_selection, y);
        _selection.Width = width;
        _selection.Height = height;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        ReleaseMouseCapture();

        var end = e.GetPosition(_canvas);
        var x = Math.Min(_start.X, end.X);
        var y = Math.Min(_start.Y, end.Y);
        var width = Math.Abs(end.X - _start.X);
        var height = Math.Abs(end.Y - _start.Y);

        SelectedBounds = new System.Drawing.Rectangle(
            (int)Math.Round(Left + x),
            (int)Math.Round(Top + y),
            (int)Math.Round(width),
            (int)Math.Round(height));

        DialogResult = true;
        Close();
    }
}
