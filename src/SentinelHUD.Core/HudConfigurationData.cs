using System.Numerics;

namespace SentinelHUD.Core;

public enum HudModuleKind
{
    Player,
    Target,
    FocusTarget,
    TargetOfTarget,
}

public enum SelfHighlightMode
{
    Off,
    Always,
    CombatOnly,
    DutyOnly,
}

public enum HighlightColourPreset
{
    Yellow,
    Green,
    Blue,
    White,
    Custom,
}

public sealed class ModuleLayoutConfiguration
{
    public float AnchorX { get; set; }
    public float AnchorY { get; set; }
}

public abstract class HudModuleConfiguration
{
    public bool Enabled { get; set; } = true;
    public float Scale { get; set; } = 1f;
    public float Opacity { get; set; } = 0.92f;
    public ModuleLayoutConfiguration Layout { get; set; } = new();
}

public sealed class PlayerModuleConfiguration : HudModuleConfiguration
{
    public bool ShowName { get; set; } = true;
    public bool ShowJob { get; set; } = true;
    public bool ShowRole { get; set; }
    public bool ShowLevel { get; set; } = true;
    public bool ShowCurrentHp { get; set; } = true;
    public bool ShowMaximumHp { get; set; } = true;
    public bool ShowHpPercentage { get; set; } = true;
    public bool ShowMp { get; set; } = true;
    public bool ShowShield { get; set; } = true;
    public bool ShowStatuses { get; set; }
}

public sealed class TargetModuleConfiguration : HudModuleConfiguration
{
    public bool ShowName { get; set; } = true;
    public bool ShowCurrentHp { get; set; } = true;
    public bool ShowMaximumHp { get; set; } = true;
    public bool ShowHpPercentage { get; set; } = true;
    public bool ShowDistance { get; set; } = true;
    public bool ShowJob { get; set; }
    public bool ShowRole { get; set; }
    public bool ShowShield { get; set; } = true;
    public bool ShowCastName { get; set; } = true;
    public bool ShowCastBar { get; set; } = true;
    public bool ShowCastPercentage { get; set; } = true;
    public bool ShowStatuses { get; set; }
}

public sealed class FocusTargetModuleConfiguration : HudModuleConfiguration
{
    public bool ShowName { get; set; } = true;
    public bool ShowCurrentHp { get; set; } = true;
    public bool ShowMaximumHp { get; set; } = true;
    public bool ShowHpPercentage { get; set; } = true;
    public bool ShowDistance { get; set; } = true;
    public bool ShowShield { get; set; } = true;
    public bool ShowCastName { get; set; } = true;
    public bool ShowCastBar { get; set; } = true;
    public bool ShowCastPercentage { get; set; } = true;
}

public sealed class TargetOfTargetModuleConfiguration : HudModuleConfiguration
{
    public bool ShowName { get; set; } = true;
    public bool ShowCurrentHp { get; set; } = true;
    public bool ShowMaximumHp { get; set; }
    public bool ShowHpPercentage { get; set; } = true;
    public bool ShowDistance { get; set; }
}

public sealed class SerializableColour
{
    public float Red { get; set; }
    public float Green { get; set; }
    public float Blue { get; set; }
    public float Alpha { get; set; } = 1f;

    public Vector4 ToVector4() => new(Red, Green, Blue, Alpha);

    public void Set(Vector4 colour)
    {
        Red = colour.X;
        Green = colour.Y;
        Blue = colour.Z;
        Alpha = colour.W;
    }
}

public sealed class SelfHighlightConfiguration
{
    public SelfHighlightMode Mode { get; set; } = SelfHighlightMode.Off;
    public HighlightColourPreset ColourPreset { get; set; } = HighlightColourPreset.Yellow;
    public SerializableColour CustomColour { get; set; } = new()
    {
        Red = 1f,
        Green = 0.82f,
        Blue = 0.22f,
        Alpha = 1f,
    };
    public float Intensity { get; set; } = 0.58f;
}

public sealed class PlayerPositionMarkerConfiguration
{
    public bool Enabled { get; set; }
    public HighlightColourPreset ColourPreset { get; set; } = HighlightColourPreset.White;
    public SerializableColour CustomColour { get; set; } = new()
    {
        Red = 1f,
        Green = 1f,
        Blue = 1f,
        Alpha = 1f,
    };
    public float Radius { get; set; } = 0.18f;
    public float Opacity { get; set; } = 0.88f;
    public bool ShowBorder { get; set; } = true;
}

public sealed class ExtendedCameraZoomConfiguration
{
    public bool Enabled { get; set; }
    public float MaximumZoomDistance { get; set; } = CameraZoomPolicy.DefaultExtendedMaximum;
}

public class HudConfigurationData
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public bool Enabled { get; set; } = true;
    public bool Locked { get; set; } = true;
    public float GlobalScale { get; set; } = 1f;
    public float GlobalOpacity { get; set; } = 1f;
    public PlayerModuleConfiguration Player { get; set; } = HudConfigurationDefaults.CreatePlayer();
    public TargetModuleConfiguration Target { get; set; } = HudConfigurationDefaults.CreateTarget();
    public FocusTargetModuleConfiguration FocusTarget { get; set; } = HudConfigurationDefaults.CreateFocusTarget();
    public TargetOfTargetModuleConfiguration TargetOfTarget { get; set; } = HudConfigurationDefaults.CreateTargetOfTarget();
    public SelfHighlightConfiguration SelfHighlight { get; set; } = new();
    public PlayerPositionMarkerConfiguration PlayerPositionMarker { get; set; } = new();
    public ExtendedCameraZoomConfiguration Camera { get; set; } = new();
}

public static class HudConfigurationDefaults
{
    public static PlayerModuleConfiguration CreatePlayer() => new()
    {
        Layout = new ModuleLayoutConfiguration { AnchorX = 0.025f, AnchorY = 0.76f },
    };

    public static TargetModuleConfiguration CreateTarget() => new()
    {
        Layout = new ModuleLayoutConfiguration { AnchorX = 0.73f, AnchorY = 0.76f },
    };

    public static FocusTargetModuleConfiguration CreateFocusTarget() => new()
    {
        Layout = new ModuleLayoutConfiguration { AnchorX = 0.73f, AnchorY = 0.53f },
        Scale = 0.92f,
    };

    public static TargetOfTargetModuleConfiguration CreateTargetOfTarget() => new()
    {
        Layout = new ModuleLayoutConfiguration { AnchorX = 0.73f, AnchorY = 0.91f },
        Scale = 0.86f,
        Opacity = 0.86f,
    };

    public static HudConfigurationData CreateAll() => new();
}

public static class HudConfigurationMigrator
{
    public static T Normalize<T>(T configuration)
        where T : HudConfigurationData
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.Player ??= HudConfigurationDefaults.CreatePlayer();
        configuration.Target ??= HudConfigurationDefaults.CreateTarget();
        configuration.FocusTarget ??= HudConfigurationDefaults.CreateFocusTarget();
        configuration.TargetOfTarget ??= HudConfigurationDefaults.CreateTargetOfTarget();
        configuration.SelfHighlight ??= new SelfHighlightConfiguration();
        configuration.PlayerPositionMarker ??= new PlayerPositionMarkerConfiguration();
        configuration.Camera ??= new ExtendedCameraZoomConfiguration();
        configuration.SelfHighlight.CustomColour ??= new SerializableColour
        {
            Red = 1f,
            Green = 0.82f,
            Blue = 0.22f,
            Alpha = 1f,
        };
        configuration.PlayerPositionMarker.CustomColour ??= new SerializableColour
        {
            Red = 1f,
            Green = 1f,
            Blue = 1f,
            Alpha = 1f,
        };

        NormalizeModule(configuration.Player, HudConfigurationDefaults.CreatePlayer().Layout);
        NormalizeModule(configuration.Target, HudConfigurationDefaults.CreateTarget().Layout);
        NormalizeModule(configuration.FocusTarget, HudConfigurationDefaults.CreateFocusTarget().Layout);
        NormalizeModule(configuration.TargetOfTarget, HudConfigurationDefaults.CreateTargetOfTarget().Layout);

        configuration.GlobalScale = ClampFinite(configuration.GlobalScale, 0.65f, 1.75f, 1f);
        configuration.GlobalOpacity = ClampFinite(configuration.GlobalOpacity, 0.2f, 1f, 1f);
        configuration.SelfHighlight.Intensity = ClampFinite(configuration.SelfHighlight.Intensity, 0.15f, 1f, 0.58f);
        configuration.PlayerPositionMarker.Radius = ClampFinite(
            configuration.PlayerPositionMarker.Radius,
            PlayerPositionMarkerPolicy.MinimumRadius,
            PlayerPositionMarkerPolicy.MaximumRadius,
            PlayerPositionMarkerPolicy.DefaultRadius);
        configuration.PlayerPositionMarker.Opacity = ClampFinite(
            configuration.PlayerPositionMarker.Opacity,
            0.1f,
            1f,
            PlayerPositionMarkerPolicy.DefaultOpacity);
        configuration.Camera.MaximumZoomDistance = CameraZoomPolicy.NormalizeMaximum(
            configuration.Camera.MaximumZoomDistance);

        var colour = configuration.SelfHighlight.CustomColour;
        colour.Red = ClampFinite(colour.Red, 0f, 1f, 1f);
        colour.Green = ClampFinite(colour.Green, 0f, 1f, 0.82f);
        colour.Blue = ClampFinite(colour.Blue, 0f, 1f, 0.22f);
        colour.Alpha = ClampFinite(colour.Alpha, 0f, 1f, 1f);

        var markerColour = configuration.PlayerPositionMarker.CustomColour;
        markerColour.Red = ClampFinite(markerColour.Red, 0f, 1f, 1f);
        markerColour.Green = ClampFinite(markerColour.Green, 0f, 1f, 1f);
        markerColour.Blue = ClampFinite(markerColour.Blue, 0f, 1f, 1f);
        markerColour.Alpha = ClampFinite(markerColour.Alpha, 0f, 1f, 1f);

        if (!Enum.IsDefined(configuration.SelfHighlight.Mode))
            configuration.SelfHighlight.Mode = SelfHighlightMode.Off;
        if (!Enum.IsDefined(configuration.SelfHighlight.ColourPreset))
            configuration.SelfHighlight.ColourPreset = HighlightColourPreset.Yellow;
        if (!Enum.IsDefined(configuration.PlayerPositionMarker.ColourPreset))
            configuration.PlayerPositionMarker.ColourPreset = HighlightColourPreset.White;

        configuration.Version = HudConfigurationData.CurrentVersion;
        return configuration;
    }

    private static void NormalizeModule(HudModuleConfiguration module, ModuleLayoutConfiguration fallback)
    {
        module.Layout ??= new ModuleLayoutConfiguration
        {
            AnchorX = fallback.AnchorX,
            AnchorY = fallback.AnchorY,
        };
        module.Scale = ClampFinite(module.Scale, 0.6f, 1.8f, 1f);
        module.Opacity = ClampFinite(module.Opacity, 0.15f, 1f, 0.92f);
        module.Layout.AnchorX = ClampFinite(module.Layout.AnchorX, 0f, 1f, fallback.AnchorX);
        module.Layout.AnchorY = ClampFinite(module.Layout.AnchorY, 0f, 1f, fallback.AnchorY);
    }

    private static float ClampFinite(float value, float minimum, float maximum, float fallback)
        => float.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;
}
