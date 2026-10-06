using Dalamud.Plugin;
using SentinelCore.Diagnostics;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

public sealed class SentinelEcosystemStatusService : IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(3);
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly DiagnosticBuffer diagnostics;
    private IReadOnlyList<SentinelPluginStatus> snapshot =
        SentinelEcosystemRegistry.Resolve(Array.Empty<SentinelPluginObservation>());
    private DateTime nextRefreshUtc = DateTime.MinValue;
    private string? lastFailureMessage;
    private bool disposed;

    public SentinelEcosystemStatusService(
        IDalamudPluginInterface pluginInterface,
        DiagnosticBuffer diagnostics)
    {
        this.pluginInterface = pluginInterface;
        this.diagnostics = diagnostics;
        pluginInterface.ActivePluginsChanged += OnActivePluginsChanged;
        Refresh(force: true);
    }

    public IReadOnlyList<SentinelPluginStatus> Snapshot => snapshot;

    public string StateDescription { get; private set; } = "Waiting for the first Dalamud plugin snapshot.";

    public void Refresh(bool force = false)
    {
        if (disposed || !force && DateTime.UtcNow < nextRefreshUtc)
            return;

        nextRefreshUtc = DateTime.UtcNow + RefreshInterval;
        try
        {
            var observed = pluginInterface.InstalledPlugins.Select(plugin =>
                new SentinelPluginObservation(
                    plugin.InternalName,
                    plugin.IsLoaded,
                    plugin.Version.ToString()));
            snapshot = SentinelEcosystemRegistry.Resolve(observed);
            var summary = SentinelEcosystemRegistry.Summarize(snapshot);
            StateDescription = $"Dalamud plugin snapshot ready: {summary.Enabled} enabled, {summary.Disabled} disabled, {summary.Total - summary.Installed} not installed.";
            lastFailureMessage = null;
        }
        catch (Exception exception)
        {
            StateDescription = "Dalamud plugin snapshot temporarily unavailable; the last successful state is retained.";
            if (!string.Equals(lastFailureMessage, exception.Message, StringComparison.Ordinal))
            {
                lastFailureMessage = exception.Message;
                diagnostics.Warning($"Sentinel ecosystem status refresh failed: {exception.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        pluginInterface.ActivePluginsChanged -= OnActivePluginsChanged;
    }

    private void OnActivePluginsChanged(IActivePluginsChangedEventArgs _)
        => nextRefreshUtc = DateTime.MinValue;
}
