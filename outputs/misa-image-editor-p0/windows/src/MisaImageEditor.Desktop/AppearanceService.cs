using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace MisaImageEditor.Desktop;

/// <summary>Persists a light, dark, or Windows-system appearance and updates WPF dynamic brushes.</summary>
public sealed class AppearanceService
{
    public const string SystemMode = "system";
    public const string Light = "light";
    public const string Dark = "dark";

    private string _mode = Light;
    private string? _settingsPath;

    public string Mode => _mode;

    public void Initialize(string applicationDataDirectory)
    {
        _settingsPath = Path.Combine(applicationDataDirectory, "theme.json");
        try
        {
            if (!File.Exists(_settingsPath)) return;
            var settings = JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(_settingsPath));
            if (settings?.Mode is Light or Dark or SystemMode) _mode = settings.Mode;
        }
        catch (IOException) { }
        catch (JsonException) { }
    }

    public void SetMode(string? mode)
    {
        _mode = mode?.ToLowerInvariant() switch
        {
            Dark => Dark,
            SystemMode => SystemMode,
            _ => Light
        };
        try
        {
            if (!string.IsNullOrWhiteSpace(_settingsPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new ThemeSettings(_mode)));
            }
        }
        catch (IOException) { }
        Apply();
    }

    public void Apply()
    {
        var isDark = string.Equals(_mode, Dark, StringComparison.Ordinal) ||
            (string.Equals(_mode, SystemMode, StringComparison.Ordinal) && IsWindowsDark());
        var palette = isDark ? DarkPalette : LightPalette;
        var resources = System.Windows.Application.Current.Resources;
        foreach (var (key, color) in palette) resources[key] = Brush(color);
    }

    private static bool IsWindowsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int lightTheme && lightTheme == 0;
        }
        catch
        {
            return false;
        }
    }

    private static SolidColorBrush Brush(string value)
    {
        var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(value));
        brush.Freeze();
        return brush;
    }

    private static readonly IReadOnlyDictionary<string, string> LightPalette = new Dictionary<string, string>
    {
        ["AppBackgroundBrush"] = "#F6F8FB",
        ["PanelBackgroundBrush"] = "#FFFFFF",
        ["SurfaceBackgroundBrush"] = "#FFFFFF",
        ["PreviewBackgroundBrush"] = "#EEF2F6",
        ["ControlBackgroundBrush"] = "#FFFFFF",
        ["ButtonBackgroundBrush"] = "#E8F0FE",
        ["BorderBrush"] = "#CBD5E1",
        ["PrimaryTextBrush"] = "#111827",
        ["SecondaryTextBrush"] = "#374151",
        ["MutedTextBrush"] = "#64748B",
        ["TopBarBackgroundBrush"] = "#FFFFFF",
    };

    private static readonly IReadOnlyDictionary<string, string> DarkPalette = new Dictionary<string, string>
    {
        ["AppBackgroundBrush"] = "#202124",
        ["PanelBackgroundBrush"] = "#292A2D",
        ["SurfaceBackgroundBrush"] = "#3C4043",
        ["PreviewBackgroundBrush"] = "#111214",
        ["ControlBackgroundBrush"] = "#202124",
        ["ButtonBackgroundBrush"] = "#3C4043",
        ["BorderBrush"] = "#5F6368",
        ["PrimaryTextBrush"] = "#ECEFF1",
        ["SecondaryTextBrush"] = "#B8C1C8",
        ["MutedTextBrush"] = "#9AA0A6",
        ["TopBarBackgroundBrush"] = "#2B2D31",
    };

    private sealed record ThemeSettings(string Mode);
}
