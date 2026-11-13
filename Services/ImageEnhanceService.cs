using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScreanX.Services
{
    public static class ImageEnhanceService
    {
        public static bool ShouldEnhance(int width, int height, int threshold = 500)
        {
            return width > 0 && height > 0 && (width <= threshold || height <= threshold);
        }

        public static Bitmap EnhanceSmallImage(Bitmap source, double factor = 2.0)
        {
            if (factor <= 1.0 || source.Width <= 0 || source.Height <= 0)
            {
                return (Bitmap)source.Clone();
            }

            int targetW = (int)Math.Round(source.Width * factor);
            int targetH = (int)Math.Round(source.Height * factor);

            // 1. High Quality Bicubic Upscaling
            var upscaled = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(upscaled))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                using var wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(source, new Rectangle(0, 0, targetW, targetH), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, wrapMode);
            }

            // 2. Fast Unsharp Mask / Edge Clarity Pass
            try
            {
                return ApplyUnsharpMask(upscaled);
            }
            catch
            {
                return upscaled;
            }
        }

        private static Bitmap ApplyUnsharpMask(Bitmap image)
        {
            int w = image.Width;
            int h = image.Height;

            var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            BitmapData srcData = image.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = result.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* srcPtr = (byte*)srcData.Scan0;
                    byte* dstPtr = (byte*)dstData.Scan0;
                    int stride = srcData.Stride;

                    // Unsharp mask weight
                    // Center 5.0, neighbors -1.0/4 -> mild edge boost
                    float centerWeight = 1.35f;
                    float neighborWeight = -0.0875f; // 4 neighbors sum to -0.35f

                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int idx = y * stride + x * 4;

                            // Edge boundaries: copy original
                            if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                            {
                                dstPtr[idx] = srcPtr[idx];         // B
                                dstPtr[idx + 1] = srcPtr[idx + 1]; // G
                                dstPtr[idx + 2] = srcPtr[idx + 2]; // R
                                dstPtr[idx + 3] = srcPtr[idx + 3]; // A
                                continue;
                            }

                            int up = (y - 1) * stride + x * 4;
                            int down = (y + 1) * stride + x * 4;
                            int left = y * stride + (x - 1) * 4;
                            int right = y * stride + (x + 1) * 4;

                            // Blue
                            float b = srcPtr[idx] * centerWeight +
                                      (srcPtr[up] + srcPtr[down] + srcPtr[left] + srcPtr[right]) * neighborWeight;

                            // Green
                            float g = srcPtr[idx + 1] * centerWeight +
                                      (srcPtr[up + 1] + srcPtr[down + 1] + srcPtr[left + 1] + srcPtr[right + 1]) * neighborWeight;

                            // Red
                            float r = srcPtr[idx + 2] * centerWeight +
                                      (srcPtr[up + 2] + srcPtr[down + 2] + srcPtr[left + 2] + srcPtr[right + 2]) * neighborWeight;

                            dstPtr[idx] = (byte)Math.Clamp(b, 0, 255);
                            dstPtr[idx + 1] = (byte)Math.Clamp(g, 0, 255);
                            dstPtr[idx + 2] = (byte)Math.Clamp(r, 0, 255);
                            dstPtr[idx + 3] = srcPtr[idx + 3]; // Alpha preserved
                        }
                    }
                }
            }
            finally
            {
                image.UnlockBits(srcData);
                result.UnlockBits(dstData);
                image.Dispose();
            }

            return result;
        }
    }
}
