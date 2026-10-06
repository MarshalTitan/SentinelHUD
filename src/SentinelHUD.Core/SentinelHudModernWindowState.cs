using System.Numerics;

namespace SentinelHUD.Core;

/// <summary>Logical window sizes, independent of ImGui and the current Dalamud UI scale.</summary>
public sealed class SentinelHudModernWindowState
{
    private Vector2? pendingSize;
    private bool nativeExpansionPending;

    public SentinelHudModernWindowState(ModernWindowConfiguration? saved = null, float headerHeight = 56f)
    {
        saved ??= new ModernWindowConfiguration();
        ExpandedSize = new Vector2(saved.ExpandedWidth, saved.ExpandedHeight);
        IsMinimized = saved.Minimized;
        if (IsMinimized)
            pendingSize = new Vector2(ExpandedSize.X, headerHeight);
    }

    public bool IsMinimized { get; private set; }
    public Vector2 ExpandedSize { get; private set; }

    public void ObserveFrame(Vector2 logicalSize)
    {
        if (!IsMinimized && float.IsFinite(logicalSize.X) && float.IsFinite(logicalSize.Y)
            && logicalSize.X > 0f && logicalSize.Y > 0f)
            ExpandedSize = logicalSize;
    }

    public void Minimize(float headerHeight)
    {
        if (!float.IsFinite(headerHeight) || headerHeight <= 0f)
            throw new ArgumentOutOfRangeException(nameof(headerHeight));
        IsMinimized = true;
        pendingSize = new Vector2(ExpandedSize.X, headerHeight);
    }

    public void Expand()
    {
        if (IsMinimized)
            pendingSize = ExpandedSize;
        IsMinimized = false;
        nativeExpansionPending = true;
    }

    public Vector2? ConsumeSizeRequest()
    {
        var request = pendingSize;
        pendingSize = null;
        return request;
    }

    public bool? ConsumeNativeExpansionRequest()
    {
        var request = nativeExpansionPending ? false : (bool?)null;
        nativeExpansionPending = false;
        return request;
    }

    public void SaveTo(ModernWindowConfiguration saved)
    {
        saved.Minimized = IsMinimized;
        saved.ExpandedWidth = ExpandedSize.X;
        saved.ExpandedHeight = ExpandedSize.Y;
    }
}
