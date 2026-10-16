using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using SentinelHUD.Core;
namespace SentinelHUD.Services;
/// <summary>v1 compatibility and preference IPC retained after utility engines moved to Hub. No game access.</summary>
public sealed class QuestConvenienceHubBridge : IDisposable
{
    private readonly IDalamudPluginInterface pi;
    private readonly AntiAfkLease lease = new();
    private readonly ICallGateProvider<int> version;
    private readonly ICallGateProvider<int> preference;
    private readonly ICallGateProvider<string, bool> claim;
    private readonly ICallGateProvider<string, bool> release;
    private bool disposed;
    public QuestConvenienceHubBridge(IDalamudPluginInterface pi, Func<int> savedPreference)
    {
        this.pi = pi;
        version = pi.GetIpcProvider<int>("SentinelHUD.Quest.ProtocolVersion");
        preference = pi.GetIpcProvider<int>("SentinelHUD.Quest.Preference");
        claim = pi.GetIpcProvider<string, bool>("SentinelHUD.Quest.TryClaim");
        release = pi.GetIpcProvider<string, bool>("SentinelHUD.Quest.Release");
        version.RegisterFunc(() => 1);
        preference.RegisterFunc(savedPreference);
        claim.RegisterFunc(token =>
        {
            var peer = ObserveHub();
            var accepted = !disposed && peer.Loaded && peer.Requested == true
                && Version.TryParse(peer.Version, out var hubVersion) && hubVersion >= new Version(0,3,0,0)
                && lease.TryClaim(token, Environment.TickCount64, true);
            return accepted;
        });
        release.RegisterFunc(token => lease.Release(token));
    }
    private (bool Loaded, string? Version, bool? Requested) ObserveHub()
    {
        try
        {
            var hub = pi.InstalledPlugins.FirstOrDefault(p => p.InternalName == "SentinelHub");
            if (hub?.IsLoaded != true) return (false, hub?.Manifest.AssemblyVersion.ToString(), false);
            bool? requested;
            try { requested = pi.GetIpcSubscriber<bool>("SentinelHub.Quest.Enabled").InvokeFunc(); }
            catch { requested = null; }
            return (true, hub.Manifest.AssemblyVersion.ToString(), requested);
        }
        catch { return (true, null, null); } // Unknown discovery must not create a second owner.
    }
    public void Dispose()
    {
        disposed = true;
        version.UnregisterFunc(); preference.UnregisterFunc(); claim.UnregisterFunc(); release.UnregisterFunc();
        lease.Clear();
    }
}
