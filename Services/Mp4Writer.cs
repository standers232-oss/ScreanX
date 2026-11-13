using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ScreanX.Services
{
    public class Mp4Writer : IVideoWriter
    {
        private readonly FileStream _fs;
        private readonly BinaryWriter _bw;
        private readonly int _width;
        private readonly int _height;
        private readonly int _fps;
        private readonly List<int> _frameSizes = new();
        private readonly List<long> _frameOffsets = new();
        private readonly List<int> _frameDurations = new();
        private readonly ImageCodecInfo _jpegCodec;
        private readonly EncoderParameters _encoderParams;
        private readonly long _mdatDataOffset;
        private long _totalDurationMs = 0;
        private bool _disposed = false;

        public Mp4Writer(string filePath, int width, int height, int fps = 25, long quality = 85)
        {
            _width = width % 2 == 0 ? width : width + 1;
            _height = height % 2 == 0 ? height : height + 1;
            _fps = Math.Max(1, fps);

            _jpegCodec = GetEncoder(ImageFormat.Jpeg);
            _encoderParams = new EncoderParameters(1);
            _encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            _fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            _bw = new BinaryWriter(_fs);

            // 1. Write 'ftyp' box (File Type Box)
            WriteFtypBox();

            // 2. Start 'mdat' box (Media Data Box)
            _mdatDataOffset = _fs.Position;
            _bw.Write(0); // placeholder size
            WriteFourCC("mdat");
        }

        public void AddFrame(Bitmap bitmap, int durationMs = 0)
        {
            if (_disposed) return;

            long offset = _fs.Position;
            _frameOffsets.Add(offset);

            using var ms = new MemoryStream();
            bitmap.Save(ms, _jpegCodec, _encoderParams);
            byte[] bytes = ms.ToArray();

            _bw.Write(bytes);
            _frameSizes.Add(bytes.Length);

            int d = durationMs > 0 ? durationMs : Math.Max(1, 1000 / _fps);
            _frameDurations.Add(d);
        }

        public void Finish(long totalDurationMs)
        {
            _totalDurationMs = totalDurationMs;
        }

        private void WriteFtypBox()
        {
            WriteInt32BE(24);
            WriteFourCC("ftyp");
            WriteFourCC("mp42"); // major_brand
            WriteInt32BE(0);     // minor_version
            WriteFourCC("mp42");
            WriteFourCC("isom");
        }

        private void WriteFourCC(string fourCC)
        {
            _bw.Write((byte)fourCC[0]);
            _bw.Write((byte)fourCC[1]);
            _bw.Write((byte)fourCC[2]);
            _bw.Write((byte)fourCC[3]);
        }

        private void WriteInt32BE(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            _bw.Write(bytes);
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            throw new InvalidOperationException("JPEG encoder not found");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                // 1. Update mdat box size
                long mdatEnd = _fs.Position;
                long mdatTotalSize = mdatEnd - _mdatDataOffset;

                _fs.Seek(_mdatDataOffset, SeekOrigin.Begin);
                WriteInt32BE((int)mdatTotalSize);
                _fs.Seek(mdatEnd, SeekOrigin.Begin);

                // 2. Write 'moov' box with accurate real-world timing
                WriteMoovBox();

                _bw.Flush();
                _fs.Flush();
                _bw.Dispose();
                _fs.Dispose();
            }
        }

        private void WriteMoovBox()
        {
            int frameCount = _frameSizes.Count;
            int timeScale = 1000;

            int duration = (int)_totalDurationMs;
            if (duration <= 0)
            {
                int sum = 0;
                foreach (var d in _frameDurations) sum += d;
                duration = sum > 0 ? sum : (frameCount > 0 ? (int)((long)frameCount * timeScale / _fps) : 0);
            }

            using var moovMs = new MemoryStream();
            using var w = new BinaryWriter(moovMs);

            void W4(string s) { w.Write((byte)s[0]); w.Write((byte)s[1]); w.Write((byte)s[2]); w.Write((byte)s[3]); }
            void WI(int v) { byte[] b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); w.Write(b); }
            void WS(short v) { byte[] b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); w.Write(b); }

            // mvhd (Movie Header Box) - 108 bytes
            WI(108); W4("mvhd");
            WI(0); // version & flags
            WI(0); // creation_time
            WI(0); // modification_time
            WI(timeScale); // timescale
            WI(duration);  // duration
            WI(0x00010000); // rate 1.0
            WS(0x0100);     // volume 1.0
            WS(0);          // reserved
            WI(0); WI(0);   // reserved
            // Matrix structure (36 bytes standard identity matrix)
            WI(0x00010000); WI(0); WI(0);
            WI(0); WI(0x00010000); WI(0);
            WI(0); WI(0); WI(0x40000000);
            for (int i = 0; i < 6; i++) WI(0); // pre-defined
            WI(2); // next_track_ID

            // trak (Track Box)
            using var trakMs = new MemoryStream();
            using var tw = new BinaryWriter(trakMs);
            void TW4(string s) { tw.Write((byte)s[0]); tw.Write((byte)s[1]); tw.Write((byte)s[2]); tw.Write((byte)s[3]); }
            void TWI(int v) { byte[] b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); tw.Write(b); }
            void TWS(short v) { byte[] b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); tw.Write(b); }

            // tkhd (Track Header Box) - 92 bytes
            TWI(92); TW4("tkhd");
            TWI(0x0000000F); // version 0, flags enabled | in_movie | in_preview
            TWI(0); // creation_time
            TWI(0); // modification_time
            TWI(1); // track_ID
            TWI(0); // reserved
            TWI(duration);
            TWI(0); TWI(0); // reserved
            TWS(0); // layer
            TWS(0); // alternate_group
            TWS(0); // volume
            TWS(0); // reserved
            // Matrix (36 bytes)
            TWI(0x00010000); TWI(0); TWI(0);
            TWI(0); TWI(0x00010000); TWI(0);
            TWI(0); TWI(0); TWI(0x40000000);
            TWI(_width << 16);  // track width in fixed point 16.16
            TWI(_height << 16); // track height in fixed point 16.16

            // mdia (Media Box)
            using var mdiaMs = new MemoryStream();
            using var mw = new BinaryWriter(mdiaMs);
            void MW4(string s) { mw.Write((byte)s[0]); mw.Write((byte)s[1]); mw.Write((byte)s[2]); mw.Write((byte)s[3]); }
            void MWI(int v) { byte[] b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); mw.Write(b); }
            void MWS(short v) { byte[] b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); mw.Write(b); }

            // mdhd (Media Header Box) - 32 bytes
            MWI(32); MW4("mdhd");
            MWI(0); // version & flags
            MWI(0); // creation_time
            MWI(0); // modification_time
            MWI(timeScale);
            MWI(duration);
            MWS(0x55C4); // language 'und'
            MWS(0);      // quality

            // hdlr (Handler Box) - 45 bytes
            MWI(45); MW4("hdlr");
            MWI(0);
            MWI(0);
            MW4("vide"); // component subtype
            MWI(0); MWI(0); MWI(0); // reserved
            mw.Write(System.Text.Encoding.ASCII.GetBytes("VideoHandler\0"));

            // minf (Media Information Box)
            using var minfMs = new MemoryStream();
            using var miw = new BinaryWriter(minfMs);
            void MIW4(string s) { miw.Write((byte)s[0]); miw.Write((byte)s[1]); miw.Write((byte)s[2]); miw.Write((byte)s[3]); }
            void MIWI(int v) { byte[] b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); miw.Write(b); }
            void MIWS(short v) { byte[] b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); miw.Write(b); }

            // vmhd (Video Media Header) - 20 bytes
            MIWI(20); MIW4("vmhd");
            MIWI(1); // version 0, flags 1
            MIWS(0); // graphicsmode
            MIWS(0); MIWS(0); MIWS(0); // opcolor

            // dinf (Data Information Box) - 36 bytes
            MIWI(36); MIW4("dinf");
            MIWI(28); MIW4("dref");
            MIWI(0); // version 0, flags 0
            MIWI(1); // entry_count 1
            MIWI(12); MIW4("url "); // self-contained url
            MIWI(1); // flags 1 (data is in same file)

            // stbl (Sample Table Box)
            using var stblMs = new MemoryStream();
            using var sw = new BinaryWriter(stblMs);
            void SW4(string s) { sw.Write((byte)s[0]); sw.Write((byte)s[1]); sw.Write((byte)s[2]); sw.Write((byte)s[3]); }
            void SWI(int v) { byte[] b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); sw.Write(b); }
            void SWS(short v) { byte[] b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); sw.Write(b); }

            // stsd (Sample Description Box)
            SWI(16 + 86); SW4("stsd");
            SWI(0); // version 0, flags 0
            SWI(1); // entry count 1
            // VisualSampleEntry ('jpeg') - 86 bytes
            SWI(86); SW4("jpeg");
            for (int i = 0; i < 6; i++) sw.Write((byte)0); // reserved
            SWS(1); // data_reference_index
            SWS(0); SWS(0); // pre_defined & reserved
            for (int i = 0; i < 3; i++) SWI(0); // pre_defined
            SWS((short)_width);
            SWS((short)_height);
            SWI(0x00480000); // 72 dpi horiz
            SWI(0x00480000); // 72 dpi vert
            SWI(0); // reserved
            SWS(1); // frame_count
            for (int i = 0; i < 32; i++) sw.Write((byte)0); // compressor name (32 bytes)
            SWS(0x0018); // depth 24
            SWS(-1); // pre_defined -1

            // stts (Time-to-Sample Box) - Real-time variable framerate compression
            var sttsEntries = new List<(int count, int delta)>();
            int currentCount = 0;
            int currentDelta = -1;

            if (_frameDurations.Count > 0)
            {
                foreach (var dur in _frameDurations)
                {
                    int delta = Math.Max(1, dur);
                    if (delta == currentDelta)
                    {
                        currentCount++;
                    }
                    else
                    {
                        if (currentCount > 0)
                        {
                            sttsEntries.Add((currentCount, currentDelta));
                        }
                        currentDelta = delta;
                        currentCount = 1;
                    }
                }
                if (currentCount > 0)
                {
                    sttsEntries.Add((currentCount, currentDelta));
                }
            }
            else
            {
                sttsEntries.Add((1, 1000));
            }

            int sttsSize = 16 + 8 * sttsEntries.Count;
            SWI(sttsSize); SW4("stts");
            SWI(0); // version & flags
            SWI(sttsEntries.Count);
            foreach (var entry in sttsEntries)
            {
                SWI(entry.count);
                SWI(entry.delta);
            }

            // stsc (Sample-to-Chunk Box)
            SWI(28); SW4("stsc");
            SWI(0);
            SWI(1); // 1 entry
            SWI(1); // first chunk
            SWI(1); // samples per chunk
            SWI(1); // sample description index

            // stsz (Sample Sizes Box)
            SWI(20 + 4 * frameCount); SW4("stsz");
            SWI(0);
            SWI(0); // sample_size 0 (variable)
            SWI(frameCount);
            foreach (var size in _frameSizes)
            {
                SWI(size);
            }

            // stco (Chunk Offsets Box)
            SWI(16 + 4 * frameCount); SW4("stco");
            SWI(0);
            SWI(frameCount);
            foreach (var offset in _frameOffsets)
            {
                SWI((int)offset);
            }

            // Assemble stbl into minf
            byte[] stblBytes = stblMs.ToArray();
            MIWI(stblBytes.Length + 8); MIW4("stbl");
            miw.Write(stblBytes);

            // Assemble minf into mdia
            byte[] minfBytes = minfMs.ToArray();
            MWI(minfBytes.Length + 8); MW4("minf");
            mw.Write(minfBytes);

            // Assemble mdia into trak
            byte[] mdiaBytes = mdiaMs.ToArray();
            TWI(mdiaBytes.Length + 8); TW4("mdia");
            tw.Write(mdiaBytes);

            // Assemble trak into moov
            byte[] trakBytes = trakMs.ToArray();
            WI(trakBytes.Length + 8); W4("trak");
            w.Write(trakBytes);

            // Write moov to main stream
            byte[] moovBytes = moovMs.ToArray();
            WriteInt32BE(moovBytes.Length + 8);
            WriteFourCC("moov");
            _bw.Write(moovBytes);
        }
    }
}
