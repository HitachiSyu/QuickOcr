using DrawingBitmap = System.Drawing.Bitmap;
using DrawingGraphics = System.Drawing.Graphics;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace QuickOcr;

public static class ScreenCapture
{
    public static DrawingBitmap Capture(DrawingRectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("The selected area is too small.");
        }

        var bitmap = new DrawingBitmap(bounds.Width, bounds.Height);
        using var graphics = DrawingGraphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, new DrawingSize(bounds.Width, bounds.Height));
        return bitmap;
    }
}
