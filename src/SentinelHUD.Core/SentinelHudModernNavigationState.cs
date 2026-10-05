namespace SentinelHUD.Core;

public enum SentinelHudModernPrimaryPage
{
    General = 0,
    Hud = 1,
    Awareness = 2,
    Systems = 3,
    Appearance = 4,
    Diagnostics = 5,
}

public enum SentinelHudModernContentPage
{
    General = 0,
    Player = 1,
    Target = 2,
    FocusTarget = 3,
    TargetOfTarget = 4,
    Layout = 5,
    Awareness = 6,
    EncounterAwareness = 7,
    Camera = 8,
    Convenience = 9,
    Appearance = 10,
    Diagnostics = 11,
}

/// <summary>
/// Retained, renderer-independent navigation state for the Sentinel Modern 2 shell.
/// Each category-heavy primary destination remembers its last selected category.
/// </summary>
public sealed class SentinelHudModernNavigationState
{
    public const string GeneralId = "general";
    public const string HudId = "hud";
    public const string AwarenessId = "awareness";
    public const string SystemsId = "systems";
    public const string AppearanceId = "appearance";
    public const string DiagnosticsId = "diagnostics";

    public SentinelHudModernPrimaryPage PrimaryPage { get; private set; }

    public SentinelHudModernContentPage HudPage { get; private set; }
        = SentinelHudModernContentPage.Player;

    public SentinelHudModernContentPage AwarenessPage { get; private set; }
        = SentinelHudModernContentPage.Awareness;

    public SentinelHudModernContentPage SystemsPage { get; private set; }
        = SentinelHudModernContentPage.Camera;

    public string PrimaryPageId => PrimaryPage switch
    {
        SentinelHudModernPrimaryPage.General => GeneralId,
        SentinelHudModernPrimaryPage.Hud => HudId,
        SentinelHudModernPrimaryPage.Awareness => AwarenessId,
        SentinelHudModernPrimaryPage.Systems => SystemsId,
        SentinelHudModernPrimaryPage.Appearance => AppearanceId,
        _ => DiagnosticsId,
    };

    public SentinelHudModernContentPage ContentPage => PrimaryPage switch
    {
        SentinelHudModernPrimaryPage.General => SentinelHudModernContentPage.General,
        SentinelHudModernPrimaryPage.Hud => HudPage,
        SentinelHudModernPrimaryPage.Awareness => AwarenessPage,
        SentinelHudModernPrimaryPage.Systems => SystemsPage,
        SentinelHudModernPrimaryPage.Appearance => SentinelHudModernContentPage.Appearance,
        _ => SentinelHudModernContentPage.Diagnostics,
    };

    public bool HasSecondaryNavigation => PrimaryPage is
        SentinelHudModernPrimaryPage.Hud
        or SentinelHudModernPrimaryPage.Awareness
        or SentinelHudModernPrimaryPage.Systems;

    public bool SelectPrimary(string id)
    {
        var next = id switch
        {
            GeneralId => SentinelHudModernPrimaryPage.General,
            HudId => SentinelHudModernPrimaryPage.Hud,
            AwarenessId => SentinelHudModernPrimaryPage.Awareness,
            SystemsId => SentinelHudModernPrimaryPage.Systems,
            AppearanceId => SentinelHudModernPrimaryPage.Appearance,
            DiagnosticsId => SentinelHudModernPrimaryPage.Diagnostics,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown primary page."),
        };

        if (next == PrimaryPage)
            return false;
        PrimaryPage = next;
        return true;
    }

    public bool SelectContent(SentinelHudModernContentPage page)
    {
        switch (page)
        {
            case SentinelHudModernContentPage.Player:
            case SentinelHudModernContentPage.Target:
            case SentinelHudModernContentPage.FocusTarget:
            case SentinelHudModernContentPage.TargetOfTarget:
            case SentinelHudModernContentPage.Layout:
                if (HudPage == page)
                    return false;
                HudPage = page;
                return true;

            case SentinelHudModernContentPage.Awareness:
            case SentinelHudModernContentPage.EncounterAwareness:
                if (AwarenessPage == page)
                    return false;
                AwarenessPage = page;
                return true;

            case SentinelHudModernContentPage.Camera:
            case SentinelHudModernContentPage.Convenience:
                if (SystemsPage == page)
                    return false;
                SystemsPage = page;
                return true;

            default:
                throw new ArgumentOutOfRangeException(nameof(page), page, "The page is not a secondary category.");
        }
    }
}
