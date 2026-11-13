using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ScreanX.Services
{
    public static class CaptureService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        public static Rectangle GetVirtualScreenBounds()
        {
            return new Rectangle(
                System.Windows.Forms.SystemInformation.VirtualScreen.Left,
                System.Windows.Forms.SystemInformation.VirtualScreen.Top,
                System.Windows.Forms.SystemInformation.VirtualScreen.Width,
                System.Windows.Forms.SystemInformation.VirtualScreen.Height
            );
        }

        public static Bitmap CaptureAllScreens()
        {
            var bounds = GetVirtualScreenBounds();
            var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }

        public static Bitmap CapturePrimaryScreen()
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds ?? System.Windows.Forms.SystemInformation.VirtualScreen;
            var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }

        public static Bitmap? CaptureActiveWindow()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return null;

            RECT rect;
            int hr = DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
            if (hr != 0)
            {
                GetWindowRect(hWnd, out rect);
            }

            int w = Math.Max(1, rect.Width);
            int h = Math.Max(1, rect.Height);

            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }

        public static Bitmap CropBitmap(Bitmap source, Rectangle cropArea)
        {
            var safeRect = new Rectangle(
                Math.Max(0, cropArea.X),
                Math.Max(0, cropArea.Y),
                Math.Min(cropArea.Width, source.Width - Math.Max(0, cropArea.X)),
                Math.Min(cropArea.Height, source.Height - Math.Max(0, cropArea.Y))
            );

            if (safeRect.Width <= 0 || safeRect.Height <= 0)
            {
                return new Bitmap(1, 1);
            }

            var cropped = new Bitmap(safeRect.Width, safeRect.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(cropped))
            {
                g.DrawImage(source, new Rectangle(0, 0, safeRect.Width, safeRect.Height), safeRect, GraphicsUnit.Pixel);
            }
            return cropped;
        }

        public static BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                BitmapSource source = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
                source.Freeze();
                return source;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        public static System.Drawing.Color GetPixelColor(int x, int y)
        {
            using var bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1), CopyPixelOperation.SourceCopy);
            return bmp.GetPixel(0, 0);
        }
    }
}
