using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace JianyingVideoAssistant.Infrastructure;

public static class ThemeManager
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private static Application? _application;

    public static bool IsDarkTheme { get; private set; }
    public static bool IsHighContrast { get; private set; }

    public static void Initialize(Application application)
    {
        _application = application;
        ApplyCurrentTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public static void Shutdown()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _application = null;
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        var application = _application;
        if (application is null || application.Dispatcher.HasShutdownStarted)
        {
            return;
        }

        _ = application.Dispatcher.BeginInvoke(new Action(ApplyCurrentTheme));
    }

    private static void ApplyCurrentTheme()
    {
        var application = _application;
        if (application is null)
        {
            return;
        }

        IsHighContrast = SystemParameters.HighContrast;
        if (IsHighContrast)
        {
            ApplyHighContrastTheme(application);
            ApplyBackdropToOpenWindows(application);
            return;
        }

        IsDarkTheme = ReadWindowsDarkTheme();
        var palette = IsDarkTheme ? DarkPalette : LightPalette;
        foreach (var (key, color) in palette)
        {
            application.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        ApplyBackdropToOpenWindows(application);
    }

    private static void ApplyBackdropToOpenWindows(Application application)
    {
        foreach (Window window in application.Windows)
        {
            WindowBackdrop.TryApply(window);
        }
    }

    private static void ApplyHighContrastTheme(Application application)
    {
        IsDarkTheme = false;
        var resources = application.Resources;
        resources["WindowBrush"] = SystemColors.WindowBrush;
        resources["RailBrush"] = SystemColors.WindowBrush;
        resources["CardBackgroundBrush"] = SystemColors.WindowBrush;
        resources["CardStrokeBrush"] = SystemColors.WindowTextBrush;
        resources["ControlBorderBrush"] = SystemColors.WindowTextBrush;
        resources["PrimaryTextBrush"] = SystemColors.WindowTextBrush;
        resources["MutedTextBrush"] = SystemColors.WindowTextBrush;
        resources["DisabledTextBrush"] = SystemColors.GrayTextBrush;
        resources["AccentBrush"] = SystemColors.HighlightBrush;
        resources["AccentHoverBrush"] = SystemColors.HighlightBrush;
        resources["AccentPressedBrush"] = SystemColors.HighlightBrush;
        resources["AccentTextBrush"] = SystemColors.HighlightTextBrush;
        resources["FocusBrush"] = SystemColors.HighlightBrush;
        resources["SuccessBrush"] = SystemColors.WindowTextBrush;
        resources["WarningBrush"] = SystemColors.WindowTextBrush;
        resources["ErrorBrush"] = SystemColors.WindowTextBrush;
        resources["InfoBrush"] = SystemColors.WindowTextBrush;
        resources["SecondaryButtonBrush"] = SystemColors.ControlBrush;
        resources["SecondaryButtonHoverBrush"] = SystemColors.ControlLightBrush;
        resources["SecondaryButtonPressedBrush"] = SystemColors.ControlDarkBrush;
        resources["DisabledBackgroundBrush"] = SystemColors.ControlBrush;
        resources["InputBrush"] = SystemColors.WindowBrush;
        resources["ComboTextBrush"] = SystemColors.WindowTextBrush;
        resources["ComboBackgroundBrush"] = SystemColors.WindowBrush;
        resources["RailSelectionBrush"] = SystemColors.WindowBrush;
        resources["BadgeBrush"] = SystemColors.WindowBrush;
        resources["SummaryTextBrush"] = SystemColors.WindowTextBrush;
        resources["OverlayBrush"] = SystemColors.ControlDarkDarkBrush;
        resources["PanelBrush"] = SystemColors.WindowBrush;
        resources["NoticeBrush"] = SystemColors.WindowBrush;
    }

    private static bool ReadWindowsDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    private static readonly IReadOnlyDictionary<string, string> LightPalette = new Dictionary<string, string>
    {
        ["WindowBrush"] = "#F2F3F3F3",
        ["RailBrush"] = "#F7FAFAFA",
        ["CardBackgroundBrush"] = "#FAFFFFFF",
        ["CardStrokeBrush"] = "#E0E0E0",
        ["ControlBorderBrush"] = "#8A8A8A",
        ["PrimaryTextBrush"] = "#242424",
        ["MutedTextBrush"] = "#616161",
        ["DisabledTextBrush"] = "#9E9E9E",
        ["AccentBrush"] = "#0F6CBD",
        ["AccentHoverBrush"] = "#115EA3",
        ["AccentPressedBrush"] = "#0C3B5E",
        ["AccentTextBrush"] = "#FFFFFF",
        ["FocusBrush"] = "#0F6CBD",
        ["SuccessBrush"] = "#107C10",
        ["WarningBrush"] = "#8A4B00",
        ["ErrorBrush"] = "#B10E1C",
        ["InfoBrush"] = "#0F6CBD",
        ["SecondaryButtonBrush"] = "#FFFFFF",
        ["SecondaryButtonHoverBrush"] = "#F5F5F5",
        ["SecondaryButtonPressedBrush"] = "#EDEDED",
        ["DisabledBackgroundBrush"] = "#F0F0F0",
        ["InputBrush"] = "#FFFFFF",
        ["ComboTextBrush"] = "#242424",
        ["ComboBackgroundBrush"] = "#FFFFFF",
        ["RailSelectionBrush"] = "#E5F1FB",
        ["BadgeBrush"] = "#E5F1FB",
        ["SummaryTextBrush"] = "#424242",
        ["OverlayBrush"] = "#66000000",
        ["PanelBrush"] = "#FFFFFF",
        ["NoticeBrush"] = "#EFF6FC"
    };

    private static readonly IReadOnlyDictionary<string, string> DarkPalette = new Dictionary<string, string>
    {
        ["WindowBrush"] = "#F5202020",
        ["RailBrush"] = "#F7181818",
        ["CardBackgroundBrush"] = "#FA2C2C2C",
        ["CardStrokeBrush"] = "#454545",
        ["ControlBorderBrush"] = "#8A8A8A",
        ["PrimaryTextBrush"] = "#FFFFFF",
        ["MutedTextBrush"] = "#D6D6D6",
        ["DisabledTextBrush"] = "#777777",
        ["AccentBrush"] = "#0F6CBD",
        ["AccentHoverBrush"] = "#115EA3",
        ["AccentPressedBrush"] = "#0C3B5E",
        ["AccentTextBrush"] = "#FFFFFF",
        ["FocusBrush"] = "#479EF5",
        ["SuccessBrush"] = "#54B054",
        ["WarningBrush"] = "#FCE100",
        ["ErrorBrush"] = "#DC626D",
        ["InfoBrush"] = "#479EF5",
        ["SecondaryButtonBrush"] = "#323232",
        ["SecondaryButtonHoverBrush"] = "#3A3A3A",
        ["SecondaryButtonPressedBrush"] = "#414141",
        ["DisabledBackgroundBrush"] = "#2A2A2A",
        ["InputBrush"] = "#2B2B2B",
        ["ComboTextBrush"] = "#FFFFFF",
        ["ComboBackgroundBrush"] = "#2B2B2B",
        ["RailSelectionBrush"] = "#0D3A58",
        ["BadgeBrush"] = "#0D3A58",
        ["SummaryTextBrush"] = "#D6E0EA",
        ["OverlayBrush"] = "#99000000",
        ["PanelBrush"] = "#292929",
        ["NoticeBrush"] = "#252F38"
    };
}
