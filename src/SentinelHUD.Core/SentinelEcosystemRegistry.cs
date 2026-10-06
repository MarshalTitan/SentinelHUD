namespace SentinelHUD.Core;

public enum SentinelCompanionState
{
    NotInstalled = 0,
    InstalledDisabled = 1,
    Enabled = 2,
}

public sealed record SentinelPluginDefinition(
    string DisplayName,
    string InternalName,
    string Description);

public readonly record struct SentinelPluginObservation(
    string InternalName,
    bool IsLoaded,
    string? Version);

public sealed record SentinelPluginStatus(
    SentinelPluginDefinition Definition,
    SentinelCompanionState State,
    string? Version);

public readonly record struct SentinelEcosystemSummary(
    int Total,
    int Installed,
    int Enabled,
    int Disabled)
{
    public string Description => Installed == Total && Disabled == 0
        ? "Your Sentinel ecosystem is ready. All known companion plugins are enabled."
        : Disabled > 0
            ? $"{Enabled} Sentinel {(Enabled == 1 ? "plugin" : "plugins")} enabled · {Disabled} installed but disabled · {Total - Installed} not installed"
            : $"{Installed} of {Total} Sentinel plugins installed · {Enabled} enabled";
}

/// <summary>
/// Canonical Sentinel HUD registry for optional, user-installed Dalamud companions.
/// Shared libraries and non-Dalamud projects intentionally do not belong here.
/// </summary>
public static class SentinelEcosystemRegistry
{
    private static readonly SentinelPluginDefinition[] Entries =
    [
        new(
            "S Rank Sentinel",
            "SRankSentinel",
            "S-rank travel, parking, tagging and recovery."),
        new(
            "PvP Sentinel",
            "PvPSentinel",
            "PvP awareness, target intelligence and combat support."),
        new(
            "Classy Sentinel",
            "ClassySentinel",
            "Controller-friendly gear-set and job launcher."),
        new(
            "Sentinel Relay",
            "SentinelRelay",
            "FFXIV and Discord chat relay."),
        new(
            "Sentinel Profiles",
            "SentinelProfiles",
            "Shared Sentinel profile and configuration management."),
    ];

    public static IReadOnlyList<SentinelPluginDefinition> Plugins => Entries;

    public static IReadOnlyList<SentinelPluginStatus> Resolve(
        IEnumerable<SentinelPluginObservation> installedPlugins)
    {
        ArgumentNullException.ThrowIfNull(installedPlugins);
        var installed = installedPlugins
            .GroupBy(plugin => plugin.InternalName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(plugin => plugin.IsLoaded).First(),
                StringComparer.OrdinalIgnoreCase);

        return Entries.Select(definition =>
        {
            if (!installed.TryGetValue(definition.InternalName, out var observed))
            {
                return new SentinelPluginStatus(
                    definition,
                    SentinelCompanionState.NotInstalled,
                    null);
            }

            return new SentinelPluginStatus(
                definition,
                observed.IsLoaded
                    ? SentinelCompanionState.Enabled
                    : SentinelCompanionState.InstalledDisabled,
                string.IsNullOrWhiteSpace(observed.Version) ? null : observed.Version);
        }).ToArray();
    }

    public static SentinelEcosystemSummary Summarize(
        IEnumerable<SentinelPluginStatus> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        var snapshot = statuses.ToArray();
        var enabled = snapshot.Count(status => status.State == SentinelCompanionState.Enabled);
        var disabled = snapshot.Count(status => status.State == SentinelCompanionState.InstalledDisabled);
        return new SentinelEcosystemSummary(
            snapshot.Length,
            enabled + disabled,
            enabled,
            disabled);
    }
}
