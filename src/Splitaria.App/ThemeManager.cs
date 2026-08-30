using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace Splitaria.App;

internal static class ThemeManager
{
    public static bool IsDark { get; private set; }
    private static AppSettings? _settings;

    public static void StartFollowingSystem(AppSettings settings)
    {
        _settings = settings;
        SystemEvents.UserPreferenceChanged -= SystemThemeChanged;
        SystemEvents.UserPreferenceChanged += SystemThemeChanged;
    }

    public static void StopFollowingSystem() => SystemEvents.UserPreferenceChanged -= SystemThemeChanged;

    public static void Apply(AppTheme preference)
    {
        IsDark = preference == AppTheme.Dark || preference == AppTheme.System && IsWindowsDark();
        Set("WindowBackgroundBrush", IsDark ? "#202024" : "#EFEFF4");
        Set("SurfaceBrush", IsDark ? "#2B2B30" : "#FFFFFF");
        Set("SubtleSurfaceBrush", IsDark ? "#333339" : "#F8F8FA");
        Set("TextBrush", IsDark ? "#F2F2F4" : "#202026");
        Set("MutedTextBrush", IsDark ? "#B8B8C2" : "#5D5D68");
        Set("ControlBackgroundBrush", IsDark ? "#36363D" : "#F5F5F8");
        Set("SoftBorderBrush", IsDark ? "#44444D" : "#E5E5EB");
        Set("HoverBrush", IsDark ? "#414148" : "#EBEBF1");
        Set("PressedBrush", IsDark ? "#4A4A52" : "#DFDFE8");
        Set("InfoBackgroundBrush", IsDark ? "#302F48" : "#F0EFFB");
        Set("InfoTextBrush", IsDark ? "#B9B5FF" : "#5751A0");
        Set("ProgressTrackBrush", IsDark ? "#44444D" : "#E3E3EA");
        Set("DisabledButtonBrush", IsDark ? "#414148" : "#DEDDE9");
        Set("DisabledTextBrush", IsDark ? "#9A9AA5" : "#777582");
        Set("SelectionBackgroundBrush", IsDark ? "#484465" : "#E3E0FA");
        Set("ViewingBackgroundBrush", IsDark ? "#303C40" : "#EDF9FC");
        Set("SelectionStatusBrush", IsDark ? "#A7E6B5" : "#246B3A");
        Set("SuccessTextBrush", IsDark ? "#9DDBAD" : "#367A4C");
        Set("WarningTextBrush", IsDark ? "#F1C27D" : "#9A5B20");
        Set("ErrorTextBrush", IsDark ? "#FFAAA3" : "#A14343");
        Set("ScrollThumbBrush", IsDark ? "#565660" : "#B8B8C3");
        Set("ScrollThumbHoverBrush", IsDark ? "#73737F" : "#8D8D9A");
        Set("ScrollThumbPressedBrush", IsDark ? "#8A85F5" : "#6F69EC");
    }

    private static void Set(string key, string color)
    {
        var converted = (Color)ColorConverter.ConvertFromString(color);
        if (Application.Current.Resources[key] is SolidColorBrush brush && !brush.IsFrozen)
            brush.Color = converted;
        else
            Application.Current.Resources[key] = new SolidColorBrush(converted);
    }

    private static bool IsWindowsDark()
    {
        try
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", 1) is int value && value == 0;
        }
        catch { return false; }
    }

    private static void SystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_settings?.Theme != AppTheme.System || Application.Current is null) return;
        Application.Current.Dispatcher.BeginInvoke(() => Apply(AppTheme.System));
    }
}
