using System;

namespace ScreanX.Models
{
    public class HistoryItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int Width { get; set; }
        public int Height { get; set; }
        public long FileSizeBytes { get; set; }

        public string FormattedTime => Timestamp.ToString("g");
        public string Dimensions => $"{Width} × {Height} px";
        public string FormattedSize => FileSizeBytes switch
        {
            < 1024 => $"{FileSizeBytes} B",
            < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
            _ => $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB"
        };
    }
}
