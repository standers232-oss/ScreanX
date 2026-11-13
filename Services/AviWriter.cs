using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ScreanX.Services
{
    public class AviWriter : IVideoWriter
    {
        private readonly FileStream _fileStream;
        private readonly BinaryWriter _writer;
        private readonly int _width;
        private readonly int _height;
        private readonly int _fps;
        private int _frameCount = 0;
        private long _moviChunkOffset;
        private long _riffSizeOffset;
        private long _avihMicrosecOffset;
        private long _totalFramesOffset;
        private long _strhScaleOffset;
        private long _strhRateOffset;
        private long _strhLengthOffset;
        private long _totalDurationMs = 0;
        private readonly ImageCodecInfo _jpegCodec;
        private readonly EncoderParameters _encoderParams;

        public AviWriter(string filePath, int width, int height, int fps = 25, long quality = 85)
        {
            _width = width % 2 == 0 ? width : width + 1; // AVI width must be even
            _height = height % 2 == 0 ? height : height + 1;
            _fps = Math.Max(1, fps);

            _jpegCodec = GetEncoder(ImageFormat.Jpeg);
            _encoderParams = new EncoderParameters(1);
            _encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            _writer = new BinaryWriter(_fileStream);

            WriteHeader();
        }

        private void WriteHeader()
        {
            // RIFF chunk
            _writer.Write(new[] { 'R', 'I', 'F', 'F' });
            _riffSizeOffset = _fileStream.Position;
            _writer.Write(0); // Will update at end
            _writer.Write(new[] { 'A', 'V', 'I', ' ' });

            // LIST hdrl
            _writer.Write(new[] { 'L', 'I', 'S', 'T' });
            long hdrlSizeOffset = _fileStream.Position;
            _writer.Write(0); // Will update
            long hdrlStart = _fileStream.Position;
            _writer.Write(new[] { 'h', 'd', 'r', 'l' });

            // avih (main header)
            _writer.Write(new[] { 'a', 'v', 'i', 'h' });
            _writer.Write(56); // avih size
            _avihMicrosecOffset = _fileStream.Position;
            _writer.Write((int)(1000000 / _fps)); // microsec per frame (updated at end)
            _writer.Write(0); // max bytes per sec
            _writer.Write(0); // padding
            _writer.Write(0x10); // flags (AVIF_HASINDEX)
            _totalFramesOffset = _fileStream.Position;
            _writer.Write(0); // total frames (update at end)
            _writer.Write(0); // initial frames
            _writer.Write(1); // streams (1 video)
            _writer.Write(0); // suggested buffer size
            _writer.Write(_width);
            _writer.Write(_height);
            _writer.Write(0); // reserved
            _writer.Write(0);
            _writer.Write(0);
            _writer.Write(0);

            // LIST strl (video stream)
            _writer.Write(new[] { 'L', 'I', 'S', 'T' });
            long strlSizeOffset = _fileStream.Position;
            _writer.Write(0);
            long strlStart = _fileStream.Position;
            _writer.Write(new[] { 's', 't', 'r', 'l' });

            // strh (stream header)
            _writer.Write(new[] { 's', 't', 'r', 'h' });
            _writer.Write(56); // strh size
            _writer.Write(new[] { 'v', 'i', 'd', 's' }); // type: vids
            _writer.Write(new[] { 'm', 'j', 'p', 'g' }); // handler: mjpg
            _writer.Write(0); // flags
            _writer.Write((short)0); // priority
            _writer.Write((short)0); // language
            _writer.Write(0); // initial frames
            _strhScaleOffset = _fileStream.Position;
            _writer.Write(1); // scale (update at end)
            _strhRateOffset = _fileStream.Position;
            _writer.Write(_fps); // rate (update at end)
            _writer.Write(0); // start
            _strhLengthOffset = _fileStream.Position;
            _writer.Write(0); // length (will update)
            _writer.Write(1024 * 1024); // buffer size
            _writer.Write(-1); // quality (-1 default)
            _writer.Write(0); // sample size
            _writer.Write((short)0); // frame left
            _writer.Write((short)0); // frame top
            _writer.Write((short)_width);
            _writer.Write((short)_height);

            // strf (BITMAPINFOHEADER)
            _writer.Write(new[] { 's', 't', 'r', 'f' });
            _writer.Write(40); // structure size
            _writer.Write(40); // biSize
            _writer.Write(_width);
            _writer.Write(_height);
            _writer.Write((short)1); // biPlanes
            _writer.Write((short)24); // biBitCount
            _writer.Write(new[] { 'M', 'J', 'P', 'G' }); // biCompression
            _writer.Write(_width * _height * 3); // biSizeImage
            _writer.Write(0); // biXPelsPerMeter
            _writer.Write(0); // biYPelsPerMeter
            _writer.Write(0); // biClrUsed
            _writer.Write(0); // biClrImportant

            // Update strl list size
            long strlEnd = _fileStream.Position;
            _fileStream.Seek(strlSizeOffset, SeekOrigin.Begin);
            _writer.Write((int)(strlEnd - strlStart));
            _fileStream.Seek(strlEnd, SeekOrigin.Begin);

            // Update hdrl list size
            long hdrlEnd = _fileStream.Position;
            _fileStream.Seek(hdrlSizeOffset, SeekOrigin.Begin);
            _writer.Write((int)(hdrlEnd - hdrlStart));
            _fileStream.Seek(hdrlEnd, SeekOrigin.Begin);

            // LIST movi
            _writer.Write(new[] { 'L', 'I', 'S', 'T' });
            _moviChunkOffset = _fileStream.Position;
            _writer.Write(0); // Will update at end
            _writer.Write(new[] { 'm', 'o', 'v', 'i' });
        }

        public void AddFrame(Bitmap frame, int durationMs = 0)
        {
            using var ms = new MemoryStream();
            if (frame.Width != _width || frame.Height != _height)
            {
                using var resized = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(resized);
                g.DrawImage(frame, 0, 0, _width, _height);
                resized.Save(ms, _jpegCodec, _encoderParams);
            }
            else
            {
                frame.Save(ms, _jpegCodec, _encoderParams);
            }

            byte[] jpegBytes = ms.ToArray();
            int length = jpegBytes.Length;
            int padding = (4 - (length % 4)) % 4;

            _writer.Write(new[] { '0', '0', 'd', 'c' });
            _writer.Write(length + padding);
            _writer.Write(jpegBytes);
            for (int i = 0; i < padding; i++)
            {
                _writer.Write((byte)0);
            }

            _frameCount++;
        }

        public void Finish(long totalDurationMs)
        {
            _totalDurationMs = totalDurationMs;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return ImageCodecInfo.GetImageEncoders()[0];
        }

        public void Close()
        {
            if (_fileStream == null || !_fileStream.CanWrite) return;

            long fileLength = _fileStream.Position;

            // Update movi size
            _fileStream.Seek(_moviChunkOffset, SeekOrigin.Begin);
            _writer.Write((int)(fileLength - _moviChunkOffset - 4));

            // Update RIFF total size
            _fileStream.Seek(_riffSizeOffset, SeekOrigin.Begin);
            _writer.Write((int)(fileLength - 8));

            // Update total frames in avih
            _fileStream.Seek(_totalFramesOffset, SeekOrigin.Begin);
            _writer.Write(_frameCount);

            // Update microsec per frame and strh rate/scale for 1:1 real-time playback
            if (_totalDurationMs > 0 && _frameCount > 0)
            {
                int microsecPerFrame = (int)Math.Max(1, _totalDurationMs * 1000 / _frameCount);
                _fileStream.Seek(_avihMicrosecOffset, SeekOrigin.Begin);
                _writer.Write(microsecPerFrame);

                _fileStream.Seek(_strhScaleOffset, SeekOrigin.Begin);
                _writer.Write((int)_totalDurationMs);

                _fileStream.Seek(_strhRateOffset, SeekOrigin.Begin);
                _writer.Write(_frameCount * 1000);

                _fileStream.Seek(_strhLengthOffset, SeekOrigin.Begin);
                _writer.Write(_frameCount);
            }

            _writer.Flush();
            _fileStream.Flush();
            _fileStream.Close();
        }

        public void Dispose()
        {
            Close();
            _encoderParams.Dispose();
            _writer.Dispose();
            _fileStream.Dispose();
        }
    }
}
