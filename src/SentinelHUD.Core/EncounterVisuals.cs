using System.Numerics;

namespace SentinelHUD.Core;

public enum EncounterColourRole { Danger, Warning, Safe, Friendly, Target, Objective }
public enum EncounterColourSource { Custom, Palette }
public enum EncounterOverlayShape { Circle, Ring, Line, Label }
public enum EncounterOverlayTest { PlayerCircle, TargetRing, TargetLine }

public sealed class EncounterPaletteConfiguration
{
    public bool Enabled { get; set; }
    public SerializableColour Danger { get; set; } = EncounterColourPolicy.Colour(1f, 0.15f, 0.1f);
    public SerializableColour Warning { get; set; } = EncounterColourPolicy.Colour(1f, 0.5f, 0.08f);
    public SerializableColour Safe { get; set; } = EncounterColourPolicy.Colour(0.2f, 0.9f, 0.4f);
    public SerializableColour Friendly { get; set; } = EncounterColourPolicy.Colour(0.2f, 0.65f, 1f);
    public SerializableColour Target { get; set; } = EncounterColourPolicy.Colour(0.7f, 0.4f, 1f);
    public SerializableColour Objective { get; set; } = EncounterColourPolicy.Colour(1f, 0.85f, 0.15f);

    public SerializableColour Get(EncounterColourRole role) => role switch
    {
        EncounterColourRole.Warning => Warning, EncounterColourRole.Safe => Safe,
        EncounterColourRole.Friendly => Friendly, EncounterColourRole.Target => Target,
        EncounterColourRole.Objective => Objective, _ => Danger,
    };
}

public sealed class EncounterColourBinding
{
    public EncounterColourSource Source { get; set; }
    public EncounterColourRole Role { get; set; }
    public SerializableColour Custom { get; set; } = EncounterColourPolicy.Colour(1f, 0.5f, 0.08f);
}

public sealed class NativeOmenColourConfiguration
{
    public bool Enabled { get; set; }
    public bool RecolourFriendly { get; set; }
    public bool CastWarningTransition { get; set; }
    public float Intensity { get; set; } = 1f;
    public float Opacity { get; set; } = 1f;
    public EncounterColourBinding Hostile { get; set; } = new() { Role = EncounterColourRole.Danger };
    public EncounterColourBinding Friendly { get; set; } = new()
    { Role = EncounterColourRole.Friendly, Custom = EncounterColourPolicy.Colour(0.2f, 0.65f, 1f) };
    public EncounterColourBinding CastWarning { get; set; } = new()
    { Role = EncounterColourRole.Danger, Custom = EncounterColourPolicy.Colour(1f, 0.1f, 0.05f) };
}

public sealed class EncounterOverlayConfiguration
{
    public bool Enabled { get; set; }
    public float Radius { get; set; } = 3f;
    public float Thickness { get; set; } = 3f;
    public float Opacity { get; set; } = 0.85f;
    public bool ShowLabels { get; set; } = true;
    public EncounterColourBinding PlayerCircle { get; set; } = new()
    { Role = EncounterColourRole.Safe, Custom = EncounterColourPolicy.Colour(0.2f, 0.9f, 0.4f) };
    public EncounterColourBinding TargetRing { get; set; } = new()
    { Role = EncounterColourRole.Target, Custom = EncounterColourPolicy.Colour(0.7f, 0.4f, 1f) };
    public EncounterColourBinding TargetLine { get; set; } = new()
    { Role = EncounterColourRole.Objective, Custom = EncounterColourPolicy.Colour(1f, 0.85f, 0.15f) };
}

public static class EncounterColourPolicy
{
    public static SerializableColour Colour(float r, float g, float b, float a = 1f)
        => new() { Red = r, Green = g, Blue = b, Alpha = a };

    public static Vector4 Resolve(EncounterColourBinding binding, EncounterPaletteConfiguration palette)
        => binding.Source == EncounterColourSource.Palette && palette.Enabled
            ? palette.Get(binding.Role).ToVector4() : binding.Custom.ToVector4();

    public static uint Pack(Vector4 colour)
    {
        static uint Byte(float value) => (uint)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);
        return Byte(colour.X) | Byte(colour.Y) << 8 | Byte(colour.Z) << 16 | Byte(colour.W) << 24;
    }

    public static Vector4 Preset(int index) => index switch
    {
        0 => new(1f, 0.12f, 0.08f, 1f), 1 => new(1f, 0.5f, 0.08f, 1f),
        2 => new(1f, 0.85f, 0.1f, 1f), 3 => new(0.2f, 0.9f, 0.4f, 1f),
        4 => new(0.2f, 0.55f, 1f, 1f), 5 => new(0.65f, 0.3f, 1f, 1f),
        _ => new(1f, 0.3f, 0.7f, 1f),
    };

    public static void Normalize(EncounterAwarenessConfiguration configuration)
    {
        configuration.Palette ??= new(); configuration.NativeColours ??= new(); configuration.Overlay ??= new();
        var paletteDefaults = new EncounterPaletteConfiguration();
        foreach (var role in Enum.GetValues<EncounterColourRole>())
        {
            var existing = configuration.Palette.Get(role);
            if (existing is null)
            {
                // Properties can be explicitly null in hand-edited or partially recovered files.
                switch (role)
                {
                    case EncounterColourRole.Danger: configuration.Palette.Danger = paletteDefaults.Danger; break;
                    case EncounterColourRole.Warning: configuration.Palette.Warning = paletteDefaults.Warning; break;
                    case EncounterColourRole.Safe: configuration.Palette.Safe = paletteDefaults.Safe; break;
                    case EncounterColourRole.Friendly: configuration.Palette.Friendly = paletteDefaults.Friendly; break;
                    case EncounterColourRole.Target: configuration.Palette.Target = paletteDefaults.Target; break;
                    case EncounterColourRole.Objective: configuration.Palette.Objective = paletteDefaults.Objective; break;
                }
            }
            NormalizeColour(configuration.Palette.Get(role), paletteDefaults.Get(role));
        }
        var native = configuration.NativeColours; var defaults = new NativeOmenColourConfiguration();
        native.Hostile ??= defaults.Hostile; native.Friendly ??= defaults.Friendly; native.CastWarning ??= defaults.CastWarning;
        NormalizeBinding(native.Hostile, defaults.Hostile); NormalizeBinding(native.Friendly, defaults.Friendly);
        NormalizeBinding(native.CastWarning, defaults.CastWarning);
        native.Intensity = Finite(native.Intensity, 0.1f, 2f, 1f); native.Opacity = Finite(native.Opacity, 0f, 1f, 1f);
        var overlay = configuration.Overlay; var overlayDefaults = new EncounterOverlayConfiguration();
        overlay.PlayerCircle ??= overlayDefaults.PlayerCircle; overlay.TargetRing ??= overlayDefaults.TargetRing;
        overlay.TargetLine ??= overlayDefaults.TargetLine;
        NormalizeBinding(overlay.PlayerCircle, overlayDefaults.PlayerCircle);
        NormalizeBinding(overlay.TargetRing, overlayDefaults.TargetRing); NormalizeBinding(overlay.TargetLine, overlayDefaults.TargetLine);
        overlay.Radius = Finite(overlay.Radius, 0.1f, 50f, 3f);
        overlay.Thickness = Finite(overlay.Thickness, 0.5f, 12f, 3f); overlay.Opacity = Finite(overlay.Opacity, 0f, 1f, 0.85f);
    }

    private static void NormalizeBinding(EncounterColourBinding binding, EncounterColourBinding fallback)
    {
        if (!Enum.IsDefined(binding.Source)) binding.Source = EncounterColourSource.Custom;
        if (!Enum.IsDefined(binding.Role)) binding.Role = fallback.Role;
        binding.Custom ??= fallback.Custom; NormalizeColour(binding.Custom, fallback.Custom);
    }

    private static void NormalizeColour(SerializableColour colour, SerializableColour fallback)
    {
        colour.Red = Finite(colour.Red, 0f, 1f, fallback.Red); colour.Green = Finite(colour.Green, 0f, 1f, fallback.Green);
        colour.Blue = Finite(colour.Blue, 0f, 1f, fallback.Blue); colour.Alpha = Finite(colour.Alpha, 0f, 1f, fallback.Alpha);
    }

    private static float Finite(float value, float min, float max, float fallback)
        => float.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
}

public readonly record struct NativeOmenStyle(bool Enabled, bool FriendlyEnabled, bool Transition,
    Vector4 Hostile, Vector4 Friendly, Vector4 Warning, Vector4 Midpoint, float Intensity, float Opacity)
{
    public bool CanRecolour(byte hostility) => Enabled && hostility <= 1 && (hostility == 1 || FriendlyEnabled);
    public static NativeOmenStyle From(NativeOmenColourConfiguration config, EncounterPaletteConfiguration palette, bool active)
        => new(active && config.Enabled, config.RecolourFriendly, config.CastWarningTransition,
            EncounterColourPolicy.Resolve(config.Hostile, palette), EncounterColourPolicy.Resolve(config.Friendly, palette),
            EncounterColourPolicy.Resolve(config.CastWarning, palette),
            config.CastWarning.Source == EncounterColourSource.Palette && palette.Enabled
                ? palette.Warning.ToVector4() : EncounterColourPolicy.Preset(1), config.Intensity, config.Opacity);

    public Vector4 Resolve(bool hostile, float? progress, float originalAlpha)
    {
        var colour = hostile ? Hostile : Friendly;
        if (hostile && Transition && progress is { } value && float.IsFinite(value))
        {
            var t = Math.Clamp(value, 0f, 1f);
            colour = t < 0.65f ? Vector4.Lerp(Hostile, Midpoint, t / 0.65f)
                : Vector4.Lerp(Midpoint, Warning, (t - 0.65f) / 0.35f);
        }
        return new Vector4(new Vector3(colour.X, colour.Y, colour.Z) * Intensity,
            colour.W * Opacity * originalAlpha);
    }
}

/// <summary>Restore only a value this writer still owns; never overwrite another colour writer.</summary>
public sealed class OmenTintLease
{
    public Vector4 Original { get; private set; }
    public Vector4 Applied { get; private set; }
    public bool HasApplied { get; private set; }
    public OmenTintLease(Vector4 original) => Original = original;
    public bool Owns(Vector4 current) => HasApplied && Vector4.DistanceSquared(current, Applied) < 0.000001f;
    public void CaptureOriginal(Vector4 original) => Original = original;
    public Vector4 Apply(Vector4 colour) { Applied = colour; HasApplied = true; return colour; }
}

/// <summary>Caller synchronizes native callbacks. A destroyed/reused pending object cannot activate.</summary>
public sealed class OmenLifetimeRegistry<T>(int maximum) where T : class
{
    private readonly Dictionary<nint, long> pending = new();
    private readonly Dictionary<nint, T> active = new();
    public int Count => active.Count;
    public IEnumerable<KeyValuePair<nint, T>> Active => active;
    public bool RegisterPending(long generation, nint address)
    {
        Forget(address);
        if (address == 0 || pending.Count + active.Count >= maximum) return false;
        pending[address] = generation; return true;
    }
    public bool IsPending(long generation, nint address)
        => pending.TryGetValue(address, out var actual) && actual == generation;
    public bool Activate(long generation, nint address, T value)
    {
        if (!IsPending(generation, address)) return false;
        pending.Remove(address); active[address] = value; return true;
    }
    public T? Find(nint address) => active.GetValueOrDefault(address);
    public void Forget(nint address) { pending.Remove(address); active.Remove(address); }
    public void Clear() { pending.Clear(); active.Clear(); }
}

public readonly record struct OmenCastTiming(uint OmenId, ulong ActorId, uint ActionId, long StartTick, float Total)
{
    public float Progress(long now) => Total > 0f ? Math.Clamp((now - StartTick) / (1000f * Total), 0f, 1f) : 0f;
    public static OmenCastTiming? Unique(ReadOnlySpan<OmenCastTiming> casts, uint omenId)
    {
        OmenCastTiming? found = null;
        foreach (var cast in casts)
        {
            if (cast.OmenId != omenId) continue;
            if (found.HasValue) return null;
            found = cast;
        }
        return found;
    }
}

public readonly record struct EncounterOverlayRequest(string Id, EncounterOverlayShape Shape,
    Vector3 Start, Vector3 End, float Radius, float Thickness, Vector4 Colour, string Label,
    uint ActorId = 0, bool FollowActor = false, bool Filled = false);

public static class EncounterOverlayPolicy
{
    public const string NamespacePrefix = "SentinelHUD.EncounterOverlay.";
    public static bool IsOwnedNamespace(string? value) => value?.StartsWith(NamespacePrefix, StringComparison.Ordinal) == true;
    public const int MaximumElements = 32;
    public const int TestDurationMilliseconds = 15000;
    public static bool Valid(EncounterOverlayRequest request)
        => !string.IsNullOrWhiteSpace(request.Id) && request.Id.Length <= 80 && Enum.IsDefined(request.Shape)
           && LayoutPolicy.IsFinite(new Vector2(request.Start.X, request.Start.Z)) && float.IsFinite(request.Start.Y)
           && LayoutPolicy.IsFinite(new Vector2(request.End.X, request.End.Z)) && float.IsFinite(request.End.Y)
           && float.IsFinite(request.Radius) && request.Radius is >= 0f and <= 50f
           && float.IsFinite(request.Thickness) && request.Thickness is >= 0.5f and <= 12f
           && float.IsFinite(request.Colour.X) && float.IsFinite(request.Colour.Y)
           && float.IsFinite(request.Colour.Z) && float.IsFinite(request.Colour.W)
           && request.Label is not null && request.Label.Length <= 120
           && (request.Shape != EncounterOverlayShape.Line || !request.FollowActor)
           && (!request.FollowActor || request.ActorId is not (0 or 0xE0000000 or uint.MaxValue));
}
