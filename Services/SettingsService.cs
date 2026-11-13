using System;
using System.IO;
using System.Text.Json;
using ScreanX.Models;

namespace ScreanX.Services
{
    public class SettingsService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreanX"
        );

        private static readonly string SettingsFile = Path.Combine(AppDataFolder, "settings.json");

        private static AppSettings? _instance;

        public static AppSettings Current
        {
            get
            {
                if (_instance == null)
                {
                    Load();
                }
                return _instance!;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    _instance = JsonSerializer.Deserialize<AppSettings>(json);
                }
            }
            catch
            {
                // Fall back to default
            }

            _instance ??= new AppSettings();

            if (!Directory.Exists(_instance.SaveDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_instance.SaveDirectory);
                }
                catch { }
            }
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
                string json = JsonSerializer.Serialize(Current, options);
                File.WriteAllText(SettingsFile, json);
            }
            catch { }
        }
    }
}
