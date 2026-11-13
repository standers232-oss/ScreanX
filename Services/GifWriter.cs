using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ScreanX.Services
{
    public class GifWriter : IVideoWriter
    {
        private readonly FileStream _fileStream;
        private readonly BinaryWriter _writer;
        private readonly int _width;
        private readonly int _height;
        private readonly int _defaultDelay;
        private bool _isFirstFrame = true;
        private bool _disposed = false;

        public GifWriter(string filePath, int width, int height, int fps = 15)
        {
            _width = width;
            _height = height;
            _defaultDelay = Math.Max(2, 100 / Math.Max(1, fps)); // In 1/100 s

            _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            _writer = new BinaryWriter(_fileStream);
        }

        public void AddFrame(Bitmap frame, int durationMs = 0)
        {
            if (_disposed) return;

            int delayInHundredths = durationMs > 0 ? Math.Max(1, (int)Math.Round(durationMs / 10.0)) : _defaultDelay;

            using var ms = new MemoryStream();
            frame.Save(ms, ImageFormat.Gif);
            byte[] gifBytes = ms.ToArray();

            if (gifBytes.Length < 13) return;

            if (_isFirstFrame)
            {
                // Write Header (GIF89a) + Logical Screen Descriptor (13 bytes)
                _writer.Write(gifBytes, 0, 13);

                int packed = gifBytes[10];
                bool hasGct = (packed & 0x80) != 0;
                int gctSize = hasGct ? 3 * (1 << ((packed & 0x07) + 1)) : 0;

                // Write Global Color Table
                if (hasGct && gifBytes.Length >= 13 + gctSize)
                {
                    _writer.Write(gifBytes, 13, gctSize);
                }

                // Write Netscape Looping Extension (infinite loop)
                _writer.Write(new byte[] {
                    0x21, 0xFF, 0x0B,
                    (byte)'N', (byte)'E', (byte)'T', (byte)'S', (byte)'C', (byte)'A', (byte)'P', (byte)'E', (byte)'2', (byte)'.', (byte)'0',
                    0x03, 0x01, 0x00, 0x00, // 0 = loop forever
                    0x00
                });

                // Write Graphic Control Extension + Image Descriptor + Data
                WriteFrameBlock(gifBytes, 13 + gctSize, delayInHundredths);
                _isFirstFrame = false;
            }
            else
            {
                int packed = gifBytes[10];
                bool hasGct = (packed & 0x80) != 0;
                int gctSize = hasGct ? 3 * (1 << ((packed & 0x07) + 1)) : 0;
                byte[]? localColorTable = hasGct ? new byte[gctSize] : null;
                if (hasGct)
                {
                    Array.Copy(gifBytes, 13, localColorTable!, 0, gctSize);
                }

                WriteFrameBlock(gifBytes, 13 + gctSize, delayInHundredths, localColorTable);
            }
        }

        public void Finish(long totalDurationMs)
        {
            // Nothing extra required for GIF
        }

        private void WriteFrameBlock(byte[] gifBytes, int dataStartOffset, int delayInHundredths, byte[]? localColorTable = null)
        {
            int imgDescIndex = -1;
            for (int i = dataStartOffset; i < gifBytes.Length - 10; i++)
            {
                if (gifBytes[i] == 0x2C)
                {
                    imgDescIndex = i;
                    break;
                }
            }

            if (imgDescIndex == -1) return;

            // 1. Write Graphic Control Extension
            byte delayLo = (byte)(delayInHundredths & 0xFF);
            byte delayHi = (byte)((delayInHundredths >> 8) & 0xFF);
            _writer.Write(new byte[] {
                0x21, 0xF9, 0x04,
                0x00, // No transparency, no disposal
                delayLo, delayHi,
                0x00, // transparent color index
                0x00  // terminator
            });

            // 2. Write Image Descriptor
            if (localColorTable != null && localColorTable.Length > 0)
            {
                byte[] desc = new byte[10];
                Array.Copy(gifBytes, imgDescIndex, desc, 0, 10);
                desc[9] = (byte)(desc[9] | 0x80 | 0x07); // Has LCT, size = 256 colors
                _writer.Write(desc);
                _writer.Write(localColorTable);
            }
            else
            {
                _writer.Write(gifBytes, imgDescIndex, 10);
            }

            // 3. Write Image Data
            int dataIndex = imgDescIndex + 10;
            if (dataIndex < gifBytes.Length)
            {
                int end = gifBytes.Length;
                if (gifBytes[^1] == 0x3B) end--; // Exclude trailer byte
                int length = end - dataIndex;
                if (length > 0)
                {
                    _writer.Write(gifBytes, dataIndex, length);
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                try
                {
                    _writer.Write((byte)0x3B); // GIF Trailer
                    _writer.Flush();
                    _fileStream.Flush();
                    _writer.Dispose();
                    _fileStream.Dispose();
                }
                catch { }
            }
        }
    }
}
