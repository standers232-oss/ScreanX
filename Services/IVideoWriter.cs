using System;
using System.Drawing;

namespace ScreanX.Services
{
    public interface IVideoWriter : IDisposable
    {
        void AddFrame(Bitmap frame, int durationMs = 0);
        void Finish(long totalDurationMs);
    }
}
