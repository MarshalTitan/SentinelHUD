using System.Collections.Concurrent;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

/// <summary>Owns intent, styling and lifetime; Splatoon owns all overlay rendering.</summary>
public sealed class EncounterVisualService : IDisposable
{
    private readonly NativeOmenColourService native;
    private readonly SplatoonOverlayBridge splatoon;
    private readonly ConcurrentQueue<Command> commands = new();
    private readonly Dictionary<string, Owned> elements = new();
    private readonly List<string> expired = new(EncounterOverlayPolicy.MaximumElements);
    private uint territory;
    private long nextPublish;
    public string NativeState => native.State;
    public int NativeOmenCount => native.TrackedCount;
    public bool OverlayReady => splatoon.Ready;
    public string OverlayState => splatoon.State;
    public string OverlayDetail => splatoon.Detail;
    public int ElementCount { get; private set; }
    public string TestState { get; private set; } = "No test elements; tests expire after 15 seconds";

    public EncounterVisualService(IObjectTable objects, IDataManager data,
        IDalamudPluginInterface plugins, IGameInteropProvider interop)
    { native = new(interop, plugins, objects, data); splatoon = new(plugins); }

    public void Test(EncounterOverlayTest test)
    { if (commands.Count < 64) commands.Enqueue(new Command(test, null, 0, false)); }
    public void ClearTests() { if (commands.Count < 64) commands.Enqueue(new Command(null, null, 0, true)); }
    public void Remove(string id)
    { if (!string.IsNullOrWhiteSpace(id) && commands.Count < 64) commands.Enqueue(new Command(null, null, 0, false, id)); }

    public bool Submit(EncounterOverlayRequest request, int durationMilliseconds = 15000)
    {
        if (!EncounterOverlayPolicy.Valid(request) || request.Id.StartsWith("test.", StringComparison.Ordinal)
            || durationMilliseconds is < 100 or > 30000 || commands.Count >= 64) return false;
        commands.Enqueue(new Command(null, request, durationMilliseconds, false)); return true;
    }

    public void Update(EncounterAwarenessConfiguration config, bool active, uint territoryId,
        IGameObject? player, IGameObject? target)
    {
        var changed = territory != territoryId;
        territory = territoryId;
        native.Update(config.NativeColours, config.Palette, active, changed);
        splatoon.UpdateConnection();
        if (!active || !config.Overlay.Enabled || changed)
        {
            splatoon.Clear(); elements.Clear(); ElementCount = 0;
            while (commands.TryDequeue(out _)) { }
            TestState = changed ? "Test elements cleared for territory change" : "Overlay disabled; no owned elements";
            return;
        }
        var now = Environment.TickCount64;
        while (commands.TryDequeue(out var command))
        {
            if (command.RemoveId is { } removeId)
            { splatoon.Remove(removeId); elements.Remove(removeId); continue; }
            if (command.ClearTests)
            {
                foreach (var pair in elements) if (pair.Value.Test.HasValue) expired.Add(pair.Key);
                RemoveExpired(); TestState = "Test elements cleared"; continue;
            }
            var id = command.Test.HasValue ? "test." + command.Test.Value : command.Request!.Value.Id;
            if (elements.Count >= EncounterOverlayPolicy.MaximumElements && !elements.ContainsKey(id))
            { TestState = "Overlay element limit reached"; continue; }
            elements[id] = new Owned(command.Test, command.Request,
                now + (command.Test.HasValue ? EncounterOverlayPolicy.TestDurationMilliseconds : command.Duration));
            TestState = command.Test.HasValue ? "Test active for 15 seconds" : "Overlay request accepted";
        }
        if (now < nextPublish) return;
        nextPublish = now + 50;
        foreach (var (id, element) in elements)
        {
            if (now >= element.ExpiresAt) { expired.Add(id); continue; }
            var request = element.Test is { } test ? BuildTest(test, config, player, target) : element.Request;
            if (request is { } actual && splatoon.Ready) splatoon.Publish(actual, element.ExpiresAt);
            else splatoon.Remove(id);
        }
        RemoveExpired(); ElementCount = elements.Count;
    }

    private static EncounterOverlayRequest? BuildTest(EncounterOverlayTest test, EncounterAwarenessConfiguration config,
        IGameObject? player, IGameObject? target)
    {
        if (player is null || !player.IsValid()) return null;
        var settings = config.Overlay;
        var binding = test switch
        {
            EncounterOverlayTest.PlayerCircle => settings.PlayerCircle,
            EncounterOverlayTest.TargetRing => settings.TargetRing, _ => settings.TargetLine,
        };
        var colour = EncounterColourPolicy.Resolve(binding, config.Palette); colour.W *= settings.Opacity;
        var id = "test." + test;
        var label = settings.ShowLabels ? test switch
        {
            EncounterOverlayTest.PlayerCircle => "Sentinel test: player", EncounterOverlayTest.TargetRing => "Sentinel test: target",
            _ => "Sentinel test: objective",
        } : string.Empty;
        if (test == EncounterOverlayTest.PlayerCircle)
            return new(id, EncounterOverlayShape.Circle, Vector3.Zero, Vector3.Zero,
                settings.Radius, settings.Thickness, colour, label, player.EntityId, true);
        if (target is null || !target.IsValid()) return null;
        return test == EncounterOverlayTest.TargetRing
            ? new(id, EncounterOverlayShape.Ring, Vector3.Zero, Vector3.Zero,
                settings.Radius, settings.Thickness, colour, label, target.EntityId, true)
            : new(id, EncounterOverlayShape.Line, player.Position, target.Position,
                0f, settings.Thickness, colour, label);
    }

    private void RemoveExpired()
    {
        foreach (var id in expired) { splatoon.Remove(id); elements.Remove(id); }
        expired.Clear();
    }
    public void Dispose()
    {
        try { native.Dispose(); }
        finally { splatoon.Dispose(); elements.Clear(); while (commands.TryDequeue(out _)) { } }
    }
    private readonly record struct Command(EncounterOverlayTest? Test, EncounterOverlayRequest? Request, int Duration, bool ClearTests, string? RemoveId = null);
    private sealed record Owned(EncounterOverlayTest? Test, EncounterOverlayRequest? Request, long ExpiresAt);
}
