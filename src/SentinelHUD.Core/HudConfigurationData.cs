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

public enum ModuleVisibilityCondition
{
    Always,
    CombatOnly,
    DutyOnly,
    CombatOrDuty,
}

public enum ShieldDisplayMode
{
    Off,
    TextOnly,
    BarOnly,
    BarAndText,
}

public enum MpDisplayMode
{
    Off,
    TextOnly,
    BarOnly,
    BarAndText,
}

public enum HpColourMode
{
    StaticRoleBased,
    HealthStateGradient,
}

public enum HudTextAlignment
{
    Left,
    Center,
    Right,
}

public enum HudNumberFormat
{
    Full,
    Compact,
}

public enum NativeTargetOverlayMode
{
    Off,
    Always,
    CombatOnly,
}

public enum NativeTargetHpFormat
{
    CurrentAndMaximum,
    Percentage,
    CurrentMaximumAndPercentage,
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
    public ModuleVisibilityCondition Visibility { get; set; } = ModuleVisibilityCondition.Always;
    public float Scale { get; set; } = 1f;
    public float Opacity { get; set; } = 0.92f;
    public float Width { get; set; } = 285f;
    public float BarHeight { get; set; } = 20f;
    public HudTextAlignment HpTextAlignment { get; set; } = HudTextAlignment.Center;
    public HudNumberFormat NumberFormat { get; set; } = HudNumberFormat.Full;
    public bool BorderEnabled { get; set; } = true;
    public float BorderOpacity { get; set; } = 0.72f;
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
    public MpDisplayMode MpDisplay { get; set; } = MpDisplayMode.BarAndText;
    public bool ShowShield { get; set; } = true;
    public ShieldDisplayMode ShieldDisplay { get; set; } = ShieldDisplayMode.BarAndText;
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
    public bool ShowLevel { get; set; } = true;
    public MpDisplayMode MpDisplay { get; set; } = MpDisplayMode.Off;
    public bool ShowShield { get; set; } = true;
    public ShieldDisplayMode ShieldDisplay { get; set; } = ShieldDisplayMode.BarAndText;
    public bool ShowCastName { get; set; } = true;
    public bool ShowCastBar { get; set; } = true;
    public bool ShowCastPercentage { get; set; } = true;
    public bool ShowStatuses { get; set; }
    public NativeTargetOverlayConfiguration NativeHpOverlay { get; set; } = new();
}

public sealed class FocusTargetModuleConfiguration : HudModuleConfiguration
{
    public bool ShowName { get; set; } = true;
    public bool ShowCurrentHp { get; set; } = true;
    public bool ShowMaximumHp { get; set; } = true;
    public bool ShowHpPercentage { get; set; } = true;
    public bool ShowDistance { get; set; } = true;
    public MpDisplayMode MpDisplay { get; set; } = MpDisplayMode.Off;
    public bool ShowShield { get; set; } = true;
    public ShieldDisplayMode ShieldDisplay { get; set; } = ShieldDisplayMode.BarAndText;
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
    // Kept as a migration bridge for schema-v2 configurations.
    public bool Enabled { get; set; }
    public SelfHighlightMode Mode { get; set; } = SelfHighlightMode.Off;
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
    public float BorderThickness { get; set; } = 1.25f;
}

public sealed class NativeTargetOverlayConfiguration
{
    public NativeTargetOverlayMode Mode { get; set; } = NativeTargetOverlayMode.Off;
    public NativeTargetHpFormat HpFormat { get; set; } = NativeTargetHpFormat.CurrentMaximumAndPercentage;
    public HudNumberFormat NumberFormat { get; set; } = HudNumberFormat.Full;
    public float OffsetX { get; set; }
    public float OffsetY { get; set; } = 4f;
}

public sealed class HudAppearanceConfiguration
{
    public HpColourMode PlayerHpColourMode { get; set; } = HpColourMode.StaticRoleBased;
    public HpColourMode TargetHpColourMode { get; set; } = HpColourMode.StaticRoleBased;
    public SerializableColour PlayerHealth { get; set; } = new() { Red = 0.28f, Green = 0.76f, Blue = 0.43f, Alpha = 1f };
    public SerializableColour FriendlyHealth { get; set; } = new() { Red = 0.30f, Green = 0.72f, Blue = 0.48f, Alpha = 1f };
    public SerializableColour HostileHealth { get; set; } = new() { Red = 0.88f, Green = 0.20f, Blue = 0.18f, Alpha = 1f };
    public SerializableColour NeutralHealth { get; set; } = new() { Red = 0.56f, Green = 0.56f, Blue = 0.60f, Alpha = 1f };
    public SerializableColour Shield { get; set; } = new() { Red = 0.18f, Green = 0.55f, Blue = 1f, Alpha = 1f };
    public SerializableColour Mp { get; set; } = new() { Red = 0.22f, Green = 0.45f, Blue = 0.92f, Alpha = 1f };
}

public sealed class ExtendedCameraZoomConfiguration
{
    public bool Enabled { get; set; }
    public float MaximumZoomDistance { get; set; } = CameraZoomPolicy.DefaultExtendedMaximum;
}

public class HudConfigurationData
{
    public const int CurrentVersion = 4;

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
    public HudAppearanceConfiguration Appearance { get; set; } = new();
}

public static class HudSizingPolicy
{
    public const float MinimumWidth = 180f;
    public const float MaximumWidth = 640f;
    public const float MinimumBarHeight = 8f;
    public const float MaximumBarHeight = 34f;
    public const float DefaultBarHeight = 20f;
}

public static class HudConfigurationDefaults
{
    public static PlayerModuleConfiguration CreatePlayer() => new()
    {
        Layout = new ModuleLayoutConfiguration { AnchorX = 0.025f, AnchorY = 0.76f },
        Width = 285f,
    };

    public static TargetModuleConfiguration CreateTarget() => new()
    {
        Layout = new ModuleLayoutConfiguration { AnchorX = 0.73f, AnchorY = 0.76f },
        Width = 320f,
        ShowJob = true,
    };

    public static FocusTargetModuleConfiguration CreateFocusTarget() => new()
    {
        Layout = new ModuleLayoutConfiguration { AnchorX = 0.73f, AnchorY = 0.53f },
        Scale = 0.92f,
        Width = 300f,
    };

    public static TargetOfTargetModuleConfiguration CreateTargetOfTarget() => new()
    {
        Layout = new ModuleLayoutConfiguration { AnchorX = 0.73f, AnchorY = 0.91f },
        Scale = 0.86f,
        Opacity = 0.86f,
        Width = 260f,
        BarHeight = 16f,
    };

    public static HudConfigurationData CreateAll() => new();
}

public static class HudConfigurationMigrator
{
    public static T Normalize<T>(T configuration)
        where T : HudConfigurationData
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var sourceVersion = configuration.Version;
        configuration.Player ??= HudConfigurationDefaults.CreatePlayer();
        configuration.Target ??= HudConfigurationDefaults.CreateTarget();
        configuration.FocusTarget ??= HudConfigurationDefaults.CreateFocusTarget();
        configuration.TargetOfTarget ??= HudConfigurationDefaults.CreateTargetOfTarget();
        configuration.SelfHighlight ??= new SelfHighlightConfiguration();
        configuration.PlayerPositionMarker ??= new PlayerPositionMarkerConfiguration();
        configuration.Camera ??= new ExtendedCameraZoomConfiguration();
        configuration.Appearance ??= new HudAppearanceConfiguration();
        configuration.Target.NativeHpOverlay ??= new NativeTargetOverlayConfiguration();
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

        configuration.Appearance.PlayerHealth ??= new HudAppearanceConfiguration().PlayerHealth;
        configuration.Appearance.FriendlyHealth ??= new HudAppearanceConfiguration().FriendlyHealth;
        configuration.Appearance.HostileHealth ??= new HudAppearanceConfiguration().HostileHealth;
        configuration.Appearance.NeutralHealth ??= new HudAppearanceConfiguration().NeutralHealth;
        configuration.Appearance.Shield ??= new HudAppearanceConfiguration().Shield;
        configuration.Appearance.Mp ??= new HudAppearanceConfiguration().Mp;

        if (sourceVersion < 3)
        {
            configuration.Player.Width = HudConfigurationDefaults.CreatePlayer().Width;
            configuration.Target.Width = HudConfigurationDefaults.CreateTarget().Width;
            configuration.FocusTarget.Width = HudConfigurationDefaults.CreateFocusTarget().Width;
            configuration.TargetOfTarget.Width = HudConfigurationDefaults.CreateTargetOfTarget().Width;
            configuration.PlayerPositionMarker.Mode = configuration.PlayerPositionMarker.Enabled
                ? SelfHighlightMode.Always
                : SelfHighlightMode.Off;
            configuration.Player.ShieldDisplay = configuration.Player.ShowShield
                ? ShieldDisplayMode.BarAndText
                : ShieldDisplayMode.Off;
            configuration.Target.ShieldDisplay = configuration.Target.ShowShield
                ? ShieldDisplayMode.BarAndText
                : ShieldDisplayMode.Off;
            configuration.FocusTarget.ShieldDisplay = configuration.FocusTarget.ShowShield
                ? ShieldDisplayMode.BarAndText
                : ShieldDisplayMode.Off;
        }

        if (sourceVersion < 4)
        {
            configuration.Player.MpDisplay = configuration.Player.ShowMp
                ? MpDisplayMode.BarAndText
                : MpDisplayMode.Off;
            configuration.Target.MpDisplay = MpDisplayMode.Off;
            configuration.FocusTarget.MpDisplay = MpDisplayMode.Off;
        }

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
        configuration.PlayerPositionMarker.BorderThickness = ClampFinite(
            configuration.PlayerPositionMarker.BorderThickness,
            PlayerPositionMarkerPolicy.MinimumBorderThickness,
            PlayerPositionMarkerPolicy.MaximumBorderThickness,
            PlayerPositionMarkerPolicy.DefaultBorderThickness);
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

        NormalizeColour(configuration.Appearance.PlayerHealth, new Vector4(0.28f, 0.76f, 0.43f, 1f));
        NormalizeColour(configuration.Appearance.FriendlyHealth, new Vector4(0.30f, 0.72f, 0.48f, 1f));
        NormalizeColour(configuration.Appearance.HostileHealth, new Vector4(0.88f, 0.20f, 0.18f, 1f));
        NormalizeColour(configuration.Appearance.NeutralHealth, new Vector4(0.56f, 0.56f, 0.60f, 1f));
        NormalizeColour(configuration.Appearance.Shield, new Vector4(0.18f, 0.55f, 1f, 1f));
        NormalizeColour(configuration.Appearance.Mp, new Vector4(0.22f, 0.45f, 0.92f, 1f));

        if (!Enum.IsDefined(configuration.SelfHighlight.Mode))
            configuration.SelfHighlight.Mode = SelfHighlightMode.Off;
        if (!Enum.IsDefined(configuration.SelfHighlight.ColourPreset))
            configuration.SelfHighlight.ColourPreset = HighlightColourPreset.Yellow;
        if (!Enum.IsDefined(configuration.PlayerPositionMarker.ColourPreset))
            configuration.PlayerPositionMarker.ColourPreset = HighlightColourPreset.White;
        if (!Enum.IsDefined(configuration.PlayerPositionMarker.Mode))
            configuration.PlayerPositionMarker.Mode = SelfHighlightMode.Off;
        if (!Enum.IsDefined(configuration.Player.ShieldDisplay))
            configuration.Player.ShieldDisplay = ShieldDisplayMode.BarAndText;
        if (!Enum.IsDefined(configuration.Target.ShieldDisplay))
            configuration.Target.ShieldDisplay = ShieldDisplayMode.BarAndText;
        if (!Enum.IsDefined(configuration.FocusTarget.ShieldDisplay))
            configuration.FocusTarget.ShieldDisplay = ShieldDisplayMode.BarAndText;
        if (!Enum.IsDefined(configuration.Player.MpDisplay))
            configuration.Player.MpDisplay = MpDisplayMode.BarAndText;
        if (!Enum.IsDefined(configuration.Target.MpDisplay))
            configuration.Target.MpDisplay = MpDisplayMode.Off;
        if (!Enum.IsDefined(configuration.FocusTarget.MpDisplay))
            configuration.FocusTarget.MpDisplay = MpDisplayMode.Off;
        if (!Enum.IsDefined(configuration.Appearance.PlayerHpColourMode))
            configuration.Appearance.PlayerHpColourMode = HpColourMode.StaticRoleBased;
        if (!Enum.IsDefined(configuration.Appearance.TargetHpColourMode))
            configuration.Appearance.TargetHpColourMode = HpColourMode.StaticRoleBased;
        if (!Enum.IsDefined(configuration.Target.NativeHpOverlay.Mode))
            configuration.Target.NativeHpOverlay.Mode = NativeTargetOverlayMode.Off;
        if (!Enum.IsDefined(configuration.Target.NativeHpOverlay.HpFormat))
            configuration.Target.NativeHpOverlay.HpFormat = NativeTargetHpFormat.CurrentMaximumAndPercentage;
        if (!Enum.IsDefined(configuration.Target.NativeHpOverlay.NumberFormat))
            configuration.Target.NativeHpOverlay.NumberFormat = HudNumberFormat.Full;

        configuration.Target.NativeHpOverlay.OffsetX = ClampFinite(configuration.Target.NativeHpOverlay.OffsetX, -500f, 500f, 0f);
        configuration.Target.NativeHpOverlay.OffsetY = ClampFinite(configuration.Target.NativeHpOverlay.OffsetY, -250f, 250f, 4f);
        configuration.PlayerPositionMarker.Enabled = configuration.PlayerPositionMarker.Mode != SelfHighlightMode.Off;
        configuration.Player.ShowMp = configuration.Player.MpDisplay != MpDisplayMode.Off;
        configuration.Player.ShowShield = configuration.Player.ShieldDisplay != ShieldDisplayMode.Off;
        configuration.Target.ShowShield = configuration.Target.ShieldDisplay != ShieldDisplayMode.Off;
        configuration.FocusTarget.ShowShield = configuration.FocusTarget.ShieldDisplay != ShieldDisplayMode.Off;

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
        module.Width = ClampFinite(module.Width, HudSizingPolicy.MinimumWidth, HudSizingPolicy.MaximumWidth, 285f);
        module.BarHeight = ClampFinite(module.BarHeight, HudSizingPolicy.MinimumBarHeight, HudSizingPolicy.MaximumBarHeight, HudSizingPolicy.DefaultBarHeight);
        module.BorderOpacity = ClampFinite(module.BorderOpacity, 0f, 1f, 0.72f);
        if (!Enum.IsDefined(module.Visibility))
            module.Visibility = ModuleVisibilityCondition.Always;
        if (!Enum.IsDefined(module.HpTextAlignment))
            module.HpTextAlignment = HudTextAlignment.Center;
        if (!Enum.IsDefined(module.NumberFormat))
            module.NumberFormat = HudNumberFormat.Full;
        module.Layout.AnchorX = ClampFinite(module.Layout.AnchorX, 0f, 1f, fallback.AnchorX);
        module.Layout.AnchorY = ClampFinite(module.Layout.AnchorY, 0f, 1f, fallback.AnchorY);
    }

    private static float ClampFinite(float value, float minimum, float maximum, float fallback)
        => float.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : fallback;

    private static void NormalizeColour(SerializableColour colour, Vector4 fallback)
    {
        colour.Red = ClampFinite(colour.Red, 0f, 1f, fallback.X);
        colour.Green = ClampFinite(colour.Green, 0f, 1f, fallback.Y);
        colour.Blue = ClampFinite(colour.Blue, 0f, 1f, fallback.Z);
        colour.Alpha = ClampFinite(colour.Alpha, 0f, 1f, fallback.W);
    }
}
