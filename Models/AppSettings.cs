using System;
using System.IO;

namespace ScreanX.Models
{
    public class AppSettings
    {
        public bool AutoCopyClipboard { get; set; } = true;
        public bool AutoSaveFile { get; set; } = true;
        public string SaveDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), 
            "ScreanX"
        );
        public string ImageFormat { get; set; } = "PNG"; // PNG, JPEG, BMP
        public int JpegQuality { get; set; } = 95;
        public string ThemeMode { get; set; } = "Dark"; // Dark, Light
        public string ColorPalette { get; set; } = "Purple"; // Purple, Teal, Blue, Orange
        public bool PlaySound { get; set; } = true;
        public bool OpenEditorAfterCapture { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimizedInTray { get; set; } = true;
        public string RegionHotkey { get; set; } = "PrintScreen";
        public string FullscreenHotkey { get; set; } = "Shift+PrintScreen";
        public string ActiveWindowHotkey { get; set; } = "Alt+PrintScreen";
        public string FileNamingPattern { get; set; } = "ScreanX_{yyyy-MM-dd_HH-mm-ss}";
        public bool AutoEnhanceSmallSnippets { get; set; } = true;
        public int SmallSnippetThreshold { get; set; } = 500; // px
        public double SmallSnippetScaleFactor { get; set; } = 2.0;

        // Video Recording Settings
        public string VideoFormat { get; set; } = "MP4"; // MP4, AVI, GIF
        public bool RecordMouseCursor { get; set; } = true;
        public bool ExcludeControlsFromRecording { get; set; } = true;
        public int RecordFps { get; set; } = 30; // 15, 24, 30, 60
        public int VideoQuality { get; set; } = 85;
    }
}
