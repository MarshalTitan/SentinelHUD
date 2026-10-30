using System.Reflection;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SentinelCore.Dalamud.Ipc;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

/// <summary>Only Splatoon's public dynamic-element surface is reflected; no private Dalamud APIs.</summary>
public sealed class SplatoonOverlayBridge : IDisposable
{
    public const string NamespacePrefix = EncounterOverlayPolicy.NamespacePrefix;
    private readonly IDalamudPluginInterface plugins;
    private readonly Dalamud.Plugin.Ipc.ICallGateSubscriber<bool> loaded;
    private readonly Dictionary<string, Published> published = new();
    private object? instance;
    private SplatoonElementContract? api;
    private long nextProbe;
    private volatile bool reconnect;
    public bool Ready { get; private set; }
    public string State { get; private set; } = "Not Available";
    public string Detail { get; private set; } = "Waiting for Splatoon";

    public SplatoonOverlayBridge(IDalamudPluginInterface plugins)
    {
        this.plugins = plugins;
        loaded = plugins.GetIpcSubscriber<bool>("Splatoon.IsLoaded");
        plugins.ActivePluginsChanged += OnPluginsChanged;
    }

    public void UpdateConnection()
    {
        var now = Environment.TickCount64;
        if (reconnect) { Clear(); Reset(); nextProbe = 0; reconnect = false; }
        if (now < nextProbe) return;
        nextProbe = now + 2000;
        var installed = plugins.InstalledPlugins.Any(p => p.IsLoaded
            && p.InternalName.Equals("Splatoon", StringComparison.OrdinalIgnoreCase));
        if (!installed) { Reset(); Detail = "Splatoon is not installed or loaded"; return; }
        var available = SafeDalamudIpc.Invoke("Splatoon.IsLoaded", () => loaded.InvokeFunc());
        if (!available.Succeeded || !available.Value) { Reset(); Detail = "Splatoon IPC is not ready"; return; }
        if (instance is not null)
        {
            var type = instance.GetType();
            if (type.GetField("Disposed")?.GetValue(instance) is false
                && ReferenceEquals(type.GetField("P", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), instance)) return;
            Reset();
        }
        try
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name!.Equals("Splatoon", StringComparison.OrdinalIgnoreCase)) continue;
                var type = assembly.GetType("Splatoon.Splatoon");
                var candidate = type?.GetField("P", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (candidate is null || type!.GetField("Init", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is not true
                    || type.GetField("Disposed")?.GetValue(candidate) is not false) continue;
                api = new SplatoonElementContract(candidate); instance = candidate;
                Ready = true; State = "Ready"; Detail = "Public dynamic-element API connected";
                return;
            }
            Reset(); Detail = "Splatoon drawing API is not available in this version";
        }
        catch (Exception exception) { Reset(); Detail = $"Splatoon drawing API unavailable ({exception.GetType().Name})"; }
    }

    public bool Publish(EncounterOverlayRequest request, long expiresAt)
    {
        if (!Ready || !EncounterOverlayPolicy.Valid(request)) return false;
        var key = NamespacePrefix + request.Id;
        try
        {
            if (!published.TryGetValue(key, out var entry))
            {
                var value = api!.Create(request);
                entry = new Published(value, request, expiresAt);
                api.Add(key, value, expiresAt);
                published[key] = entry;
            }
            else
            {
                if (entry.Request.Shape != request.Shape || entry.Request.FollowActor != request.FollowActor || entry.ExpiresAt != expiresAt)
                { Remove(request.Id); return Publish(request, expiresAt); }
                if (entry.Request != request)
                { api!.Update(entry.Element, request); entry.Request = request; }
            }
            return true;
        }
        catch (Exception exception)
        {
            Clear(); Reset(); nextProbe = Environment.TickCount64 + 2000;
            Detail = $"Splatoon drawing unavailable ({exception.GetType().Name})";
            return false;
        }
    }

    public void Remove(string id)
    {
        var key = NamespacePrefix + id;
        if (!published.Remove(key)) return;
        try { api?.Remove(key); } catch { /* The renderer may already be unloaded. */ }
    }

    public void Clear()
    {
        foreach (var key in published.Keys)
            try { api?.Remove(key); } catch { /* Finite expiry remains the final fallback. */ }
        published.Clear();
    }

    private void Reset()
    {
        Ready = false; State = "Not Available"; published.Clear();
        instance = null; api = null;
    }
    private void OnPluginsChanged(IActivePluginsChangedEventArgs _) => reconnect = true;
    public void Dispose() { plugins.ActivePluginsChanged -= OnPluginsChanged; Clear(); Reset(); }
    private sealed class Published(object element, EncounterOverlayRequest request, long expiry)
    {
        public object Element { get; } = element;
        public EncounterOverlayRequest Request = request;
        public long ExpiresAt { get; } = expiry;
    }
}
