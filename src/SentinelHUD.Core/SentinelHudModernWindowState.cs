namespace SentinelHUD.Core;

/// <summary>
/// Transient chrome state. Collapse requests are applied once, before the top-level Begin,
/// so ImGui remains free to restore the minimized native title strip interactively.
/// Window geometry and persistent plugin configuration remain owned by their existing hosts.
/// </summary>
public sealed class SentinelHudModernWindowState
{
    private bool? pendingCollapse;

    public bool IsMinimized { get; private set; }

    public void Minimize()
    {
        IsMinimized = true;
        pendingCollapse = true;
    }

    public void Expand()
    {
        IsMinimized = false;
        pendingCollapse = false;
    }

    public bool? ConsumeCollapseRequest()
    {
        var request = pendingCollapse;
        pendingCollapse = null;
        return request;
    }

    // Dalamud only invokes Draw when Begin reports expanded content. A native restore
    // therefore returns to the custom Core header on the following frame.
    public void ObserveExpanded() => IsMinimized = false;
}
