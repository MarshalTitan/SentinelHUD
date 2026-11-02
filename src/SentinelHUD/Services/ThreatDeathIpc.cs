using System.Text.Json;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using SentinelHUD.Core;
namespace SentinelHUD.Services;

// Immutable JSON is rebuilt only on death/reset, never on live samples or subscriber reads.
public sealed class ThreatDeathIpc : IDisposable
{
    private readonly ThreatDeathHistory history;
    private readonly IClientState client;
    private readonly ICallGateProvider<int> version;
    private readonly ICallGateProvider<string> snapshot;
    private readonly ICallGateProvider<long, object?> changed;
    private string json = "";
    private long revision;
    private bool disposed;
    public ThreatDeathIpc(IDalamudPluginInterface pi, IClientState client, ThreatDeathHistory history)
    {
        this.history = history; this.client = client;
        version = pi.GetIpcProvider<int>("SentinelHUD.Deaths.v1.Version");
        snapshot = pi.GetIpcProvider<string>("SentinelHUD.Deaths.v1.Snapshot");
        changed = pi.GetIpcProvider<long, object?>("SentinelHUD.Deaths.v1.Changed");
        Rebuild(); version.RegisterFunc(() => 1); snapshot.RegisterFunc(() => json);
        history.Changed += Rebuild;
        client.Login += Reset; client.Logout += Logout; client.TerritoryChanged += Territory;
    }
    private void Reset() => history.Reset();
    private void Logout(int _, int __) => Reset();
    private void Territory(uint _) => Reset();
    private void Rebuild()
    {
        json = JsonSerializer.Serialize(new ThreatEnvelope(1, history.Session, history.Deaths.ToArray()));
        try { changed.SendMessage(++revision); } catch { /* A subscriber cannot break the counter. */ }
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        history.Changed -= Rebuild;
        client.Login -= Reset; client.Logout -= Logout; client.TerritoryChanged -= Territory;
        version.UnregisterFunc(); snapshot.UnregisterFunc(); history.Reset();
    }
}
