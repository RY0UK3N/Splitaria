using Splitaria.Core;
using System.IO;
using System.Text.Json;

namespace Splitaria.App;

public enum AppTheme { System, Light, Dark }

public sealed class AppSettings
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Splitaria");
    private static readonly string SettingsPath = Path.Combine(SettingsFolder, "settings.json");

    public AppTheme Theme { get; set; } = AppTheme.System;
    public bool AutoPlayVideoPreview { get; set; } = true;
    public FolderPattern FolderPattern { get; set; } = FolderPattern.YearAndNamedMonth;
    public bool HasWindowPlacement { get; set; }
    public double WindowWidth { get; set; } = 1040;
    public double WindowHeight { get; set; } = 680;
    public double WindowLeft { get; set; }
    public double WindowTop { get; set; }
    public bool WindowWasMaximized { get; set; }
    public int VideoVolume { get; set; } = 60;
    public bool VideoMuted { get; set; }

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings()
                : new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsFolder);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
