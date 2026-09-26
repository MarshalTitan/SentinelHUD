using System.Text.Json;
using Dalamud.Plugin;
using SentinelCore.Configuration;
using SentinelCore.Diagnostics;
using SentinelHUD.Core;

namespace SentinelHUD.Persistence;

/// <summary>
/// Protects the user's Dalamud configuration from a transient/null typed load during a live plugin update.
/// A schema backup is made once before migration, and an unreadable document is backed up before defaults
/// can be persisted. The raw JSON fallback also avoids old assembly type metadata breaking an in-game update.
/// </summary>
public sealed class ResilientConfigurationStore : IConfigurationStore<global::SentinelHUD.Configuration>
{
    private static readonly JsonSerializerOptions RawJsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ISentinelLogger diagnostics;
    private readonly ISentinelLogger logger;

    public ResilientConfigurationStore(
        IDalamudPluginInterface pluginInterface,
        ISentinelLogger diagnostics,
        ISentinelLogger logger)
    {
        this.pluginInterface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface));
        this.diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool ConfigurationFileExisted { get; private set; }
    public bool UsedRawFallback { get; private set; }
    public bool LoadFailed { get; private set; }
    public int? SourceVersion { get; private set; }
    public string? BackupPath { get; private set; }
    public string StateDescription { get; private set; } = "Configuration has not been loaded.";

    public global::SentinelHUD.Configuration? Load()
    {
        var file = pluginInterface.ConfigFile;
        file.Refresh();
        ConfigurationFileExisted = file.Exists;
        if (!file.Exists)
        {
            StateDescription = "No previous configuration file; created schema defaults.";
            return null;
        }

        string raw;
        try
        {
            raw = File.ReadAllText(file.FullName);
            SourceVersion = ReadVersion(raw);
            if (SourceVersion.GetValueOrDefault() < HudConfigurationData.CurrentVersion)
                BackupPath = CreateBackupOnce(file, $"schema-v{SourceVersion.GetValueOrDefault()}");
        }
        catch (Exception exception)
        {
            LoadFailed = true;
            BackupPath = CreateBackupOnce(file, "failed-read");
            ReportFailure("The existing configuration could not be read; safe defaults will be used.", exception);
            return null;
        }

        try
        {
            if (pluginInterface.GetPluginConfig() is global::SentinelHUD.Configuration typed)
            {
                StateDescription = SourceVersion.GetValueOrDefault() < HudConfigurationData.CurrentVersion
                    ? $"Loaded schema {SourceVersion.GetValueOrDefault()} with migration backup."
                    : $"Loaded schema {SourceVersion.GetValueOrDefault(HudConfigurationData.CurrentVersion)}.";
                return typed;
            }
        }
        catch (Exception exception)
        {
            diagnostics.Warning("Dalamud's typed configuration load failed; trying the protected raw JSON fallback.", exception);
            logger.Warning("Sentinel HUD typed configuration load failed; trying raw JSON fallback.", exception);
        }

        try
        {
            var fallback = JsonSerializer.Deserialize<global::SentinelHUD.Configuration>(raw, RawJsonOptions);
            if (fallback is not null)
            {
                UsedRawFallback = true;
                StateDescription = "Recovered the existing configuration through the raw JSON update fallback.";
                diagnostics.Warning(StateDescription);
                logger.Warning(StateDescription);
                return fallback;
            }
        }
        catch (Exception exception)
        {
            diagnostics.Warning("The protected raw JSON configuration fallback also failed.", exception);
            logger.Warning("Sentinel HUD raw JSON configuration fallback failed.", exception);
        }

        LoadFailed = true;
        BackupPath ??= CreateBackupOnce(file, "failed-load");
        var failure = "Existing settings were unreadable. A recovery backup was preserved before safe defaults were loaded.";
        diagnostics.Error(failure);
        logger.Error(failure);
        StateDescription = failure;
        return null;
    }

    public void Save(global::SentinelHUD.Configuration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (LoadFailed && ConfigurationFileExisted && BackupPath is null)
            throw new IOException("Refusing to overwrite an unreadable Sentinel HUD configuration because its recovery backup could not be created.");
        pluginInterface.SavePluginConfig(configuration);
    }

    private static int? ReadVersion(string raw)
    {
        using var document = JsonDocument.Parse(raw, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });
        return document.RootElement.TryGetProperty(nameof(HudConfigurationData.Version), out var version)
               && version.TryGetInt32(out var value)
            ? value
            : 0;
    }

    private string? CreateBackupOnce(FileInfo source, string reason)
    {
        try
        {
            var name = $"{Path.GetFileNameWithoutExtension(source.Name)}.{reason}.backup.json";
            var destination = Path.Combine(source.DirectoryName ?? pluginInterface.ConfigDirectory.FullName, name);
            if (!File.Exists(destination))
                File.Copy(source.FullName, destination, overwrite: false);
            diagnostics.Information($"Preserved configuration backup: {destination}");
            logger.Information($"Sentinel HUD preserved configuration backup: {destination}");
            return destination;
        }
        catch (Exception exception)
        {
            diagnostics.Error("Could not preserve a configuration backup.", exception);
            logger.Error("Sentinel HUD could not preserve a configuration backup.", exception);
            return null;
        }
    }

    private void ReportFailure(string message, Exception exception)
    {
        StateDescription = message;
        diagnostics.Error(message, exception);
        logger.Error($"Sentinel HUD: {message}", exception);
    }
}
