using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ScreanX.Services
{
    public class ScreenRecordService
    {
        private static ScreenRecordService? _instance;
        public static ScreenRecordService Current => _instance ??= new ScreenRecordService();

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("user32.dll")]
        private static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyWidth, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int CURSOR_SHOWING = 0x00000001;
        private const int DI_NORMAL = 0x0003;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        private CancellationTokenSource? _cts;
        private Task? _captureTask;
        private Task? _encodeTask;
        private BlockingCollection<(Bitmap frame, int durationMs)>? _frameQueue;
        private IVideoWriter? _writer;
        private string? _currentFilePath;
        private Rectangle _recordArea;
        private Stopwatch? _recordStopwatch;

        public bool IsRecording { get; private set; }
        public bool IsPaused { get; private set; }
        public TimeSpan ElapsedTime { get; private set; } = TimeSpan.Zero;

        public event Action<TimeSpan>? Tick;
        public event Action<string>? Completed;

        public void Start(Rectangle area, string outputPath, int fps = 30)
        {
            if (IsRecording) return;

            _recordArea = area;
            _currentFilePath = outputPath;

            var settings = SettingsService.Current;
            string format = settings.VideoFormat.ToUpperInvariant();

            try
            {
                _writer = format switch
                {
                    "GIF" => new GifWriter(outputPath, area.Width, area.Height, fps),
                    "AVI" => new AviWriter(outputPath, area.Width, area.Height, fps, settings.VideoQuality),
                    _ => new Mp4Writer(outputPath, area.Width, area.Height, fps, settings.VideoQuality)
                };
            }
            catch
            {
                _writer = new AviWriter(outputPath, area.Width, area.Height, fps, settings.VideoQuality);
            }

            _cts = new CancellationTokenSource();
            _frameQueue = new BlockingCollection<(Bitmap frame, int durationMs)>(new ConcurrentQueue<(Bitmap frame, int durationMs)>(), 150);
            IsRecording = true;
            IsPaused = false;
            ElapsedTime = TimeSpan.Zero;

            var token = _cts.Token;

            // Start async background encoding worker
            _encodeTask = Task.Run(() => EncodeWorker(token), token);

            // Start dedicated high-precision capture loop
            _captureTask = Task.Run(() => CaptureLoop(fps, token), token);
        }

        private void CaptureLoop(int fps, CancellationToken token)
        {
            int targetIntervalMs = Math.Max(10, 1000 / fps);
            _recordStopwatch = Stopwatch.StartNew();
            var timerStopwatch = Stopwatch.StartNew();

            long nextFrameTargetMs = 0;
            long lastFrameTimestamp = 0;
            bool recordCursor = SettingsService.Current.RecordMouseCursor;

            while (!token.IsCancellationRequested)
            {
                if (!IsPaused)
                {
                    long currentMs = _recordStopwatch.ElapsedMilliseconds;

                    if (currentMs >= nextFrameTargetMs)
                    {
                        try
                        {
                            var frameBmp = new Bitmap(_recordArea.Width, _recordArea.Height, PixelFormat.Format32bppArgb);
                            using (var g = Graphics.FromImage(frameBmp))
                            {
                                g.CopyFromScreen(_recordArea.Left, _recordArea.Top, 0, 0, _recordArea.Size, CopyPixelOperation.SourceCopy);

                                if (recordCursor)
                                {
                                    DrawCursorOnFrame(g);
                                }
                            }

                            int durationMs = lastFrameTimestamp == 0 ? targetIntervalMs : (int)(currentMs - lastFrameTimestamp);
                            lastFrameTimestamp = currentMs;

                            if (_frameQueue != null && !_frameQueue.IsAddingCompleted)
                            {
                                // If queue is full (system heavily overloaded), drop oldest or skip to prevent memory leak
                                if (_frameQueue.Count < 140)
                                {
                                    _frameQueue.Add((frameBmp, durationMs));
                                }
                                else
                                {
                                    frameBmp.Dispose();
                                }
                            }
                            else
                            {
                                frameBmp.Dispose();
                            }
                        }
                        catch { }

                        nextFrameTargetMs += targetIntervalMs;
                        // Catch up if falling behind significantly
                        if (currentMs - nextFrameTargetMs > targetIntervalMs * 3)
                        {
                            nextFrameTargetMs = currentMs + targetIntervalMs;
                        }
                    }

                    if (timerStopwatch.ElapsedMilliseconds >= 1000)
                    {
                        ElapsedTime = ElapsedTime.Add(TimeSpan.FromSeconds(1));
                        timerStopwatch.Restart();
                        Tick?.Invoke(ElapsedTime);
                    }
                }
                else
                {
                    lastFrameTimestamp = _recordStopwatch.ElapsedMilliseconds;
                }

                Thread.Sleep(1);
            }
        }

        private void EncodeWorker(CancellationToken token)
        {
            if (_frameQueue == null) return;

            try
            {
                while (!_frameQueue.IsCompleted)
                {
                    if (_frameQueue.TryTake(out var item, 100))
                    {
                        try
                        {
                            _writer?.AddFrame(item.frame, item.durationMs);
                        }
                        catch { }
                        finally
                        {
                            item.frame.Dispose();
                        }
                    }
                }
            }
            catch { }
        }

        private void DrawCursorOnFrame(Graphics g)
        {
            try
            {
                var ci = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
                if (GetCursorInfo(out ci) && ci.flags == CURSOR_SHOWING && ci.hCursor != IntPtr.Zero)
                {
                    int x = ci.ptScreenPos.x - _recordArea.Left;
                    int y = ci.ptScreenPos.y - _recordArea.Top;

                    if (x >= -32 && y >= -32 && x < _recordArea.Width + 32 && y < _recordArea.Height + 32)
                    {
                        if (GetIconInfo(ci.hCursor, out var info))
                        {
                            x -= info.xHotspot;
                            y -= info.yHotspot;
                            if (info.hbmMask != IntPtr.Zero) DeleteObject(info.hbmMask);
                            if (info.hbmColor != IntPtr.Zero) DeleteObject(info.hbmColor);
                        }

                        IntPtr hdc = g.GetHdc();
                        try
                        {
                            DrawIconEx(hdc, x, y, ci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                        }
                        finally
                        {
                            g.ReleaseHdc(hdc);
                        }
                    }
                }
            }
            catch { }
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void Stop()
        {
            if (!IsRecording) return;

            long totalDurationMs = _recordStopwatch?.ElapsedMilliseconds ?? 1000;
            _cts?.Cancel();

            try { _captureTask?.Wait(1500); } catch { }

            // Mark queue complete and flush remaining frames to encoder
            _frameQueue?.CompleteAdding();
            try { _encodeTask?.Wait(4000); } catch { }

            _writer?.Finish(totalDurationMs);
            _writer?.Dispose();
            _writer = null;

            _frameQueue?.Dispose();
            _frameQueue = null;

            IsRecording = false;
            IsPaused = false;

            if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
            {
                Completed?.Invoke(_currentFilePath);
            }
        }

        public void Cancel()
        {
            if (!IsRecording) return;

            _cts?.Cancel();
            try { _captureTask?.Wait(1000); } catch { }

            _frameQueue?.CompleteAdding();
            try { _encodeTask?.Wait(1000); } catch { }

            _writer?.Dispose();
            _writer = null;

            if (_frameQueue != null)
            {
                while (_frameQueue.TryTake(out var item))
                {
                    item.frame.Dispose();
                }
                _frameQueue.Dispose();
                _frameQueue = null;
            }

            IsRecording = false;
            IsPaused = false;

            if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
            {
                try { File.Delete(_currentFilePath); } catch { }
            }
        }
    }
}
