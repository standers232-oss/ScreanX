using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using ScreanX.Models;

namespace ScreanX.Services
{
    public static class HistoryService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreanX"
        );

        private static readonly string HistoryFile = Path.Combine(AppDataFolder, "history.json");

        public static ObservableCollection<HistoryItem> Items { get; } = new();

        public static void Load()
        {
            try
            {
                Items.Clear();
                if (File.Exists(HistoryFile))
                {
                    string json = File.ReadAllText(HistoryFile);
                    var list = JsonSerializer.Deserialize<ObservableCollection<HistoryItem>>(json);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            if (File.Exists(item.FilePath))
                            {
                                Items.Add(item);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Items, options);
                File.WriteAllText(HistoryFile, json);
            }
            catch { }
        }

        public static void AddItem(string filePath, int width, int height)
        {
            try
            {
                var fi = new FileInfo(filePath);
                var item = new HistoryItem
                {
                    FilePath = filePath,
                    FileName = fi.Name,
                    Timestamp = DateTime.Now,
                    Width = width,
                    Height = height,
                    FileSizeBytes = fi.Exists ? fi.Length : 0
                };

                // Add at top
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Insert(0, item);
                    while (Items.Count > 100) // Keep latest 100
                    {
                        Items.RemoveAt(Items.Count - 1);
                    }
                });

                Save();
            }
            catch { }
        }

        public static void RemoveItem(HistoryItem item)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Remove(item);
                });
                Save();
            }
            catch { }
        }

        public static void ClearAll()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                });
                Save();
            }
            catch { }
        }
    }
}
