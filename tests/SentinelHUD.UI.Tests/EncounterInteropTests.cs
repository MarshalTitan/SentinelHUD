using System.Numerics;
using System.Runtime.InteropServices;
using SentinelHUD.Core;
using SentinelHUD.Services;

internal static class EncounterInteropTests
{
    private static void Check(bool value) { if (!value) throw new InvalidOperationException("Encounter interop assertion failed"); }

    public static unsafe void Memory()
    {
        var handle = Marshal.AllocHGlobal(0x1E0); var resource = Marshal.AllocHGlobal(0xD0);
        try
        {
            new Span<byte>((void*)handle, 0x1E0).Clear(); new Span<byte>((void*)resource, 0xD0).Clear();
            Check(!NativeOmenMemory.TryGetInstance(0, out _) && !NativeOmenMemory.TryGetInstance(-1, out _));
            Check(!NativeOmenMemory.TryGetInstance(handle, out _));
            // Synthetic client ABI, including independent data handle and render-resource allocations.
            Marshal.WriteIntPtr(handle, 0x1B8, resource);
            var original = new Vector4(1f, 0.8f, 0.7f, 0.6f);
            *(Vector4*)((byte*)resource + 0xA0) = original;
            Check(NativeOmenMemory.TryGetInstance(handle, out var resolved) && resolved == resource);
            Check(NativeOmenMemory.Read(resolved) == original);
            var lease = new OmenTintLease(original);
            NativeOmenMemory.Write(resolved, lease.Apply(new Vector4(0.1f, 0.2f, 1f, 0.3f)));
            Check(lease.Owns(*(Vector4*)((byte*)resource + 0xA0)));
            NativeOmenMemory.Write(resolved, lease.Original);
            Check(*(Vector4*)((byte*)resource + 0xA0) == original);
            Marshal.WriteIntPtr(handle, 0x1B8, -1);
            Check(!NativeOmenMemory.TryGetInstance(handle, out _));
        }
        finally { Marshal.FreeHGlobal(resource); Marshal.FreeHGlobal(handle); }
    }

    public static void SplatoonContract()
    {
        var renderer = new PublicRenderer(); var api = new SplatoonElementContract(renderer);
        var request = new EncounterOverlayRequest("test.world", EncounterOverlayShape.Circle,
            new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f), 3f, 4f, new Vector4(1f, 0f, 0f, 0.5f), "Sentinel");
        var element = (PublicElement)api.Create(request);
        Check(element.type == 0 && element.Enabled && !element.Filled);
        Check(element.refX == 1f && element.refY == 3f && element.refZ == 2f);
        Check(element.offX == 4f && element.offY == 6f && element.offZ == 5f);
        Check(element.radius == 3f && element.thicc == 4f && element.overlayText == "Sentinel");
        Check(element.color == EncounterColourPolicy.Pack(request.Colour));
        const string key = "SentinelHUD.EncounterOverlay.test.world";
        api.Add(key, element, 123456);
        Check(renderer.Groups[key].Expiry.SequenceEqual(new long[] { 123456, -2 }));
        api.Update(element, request with { Radius = 12f, Label = "Changed" });
        Check(ReferenceEquals(renderer.Groups[key].Elements[0], element) && element.radius == 12f && element.overlayText == "Changed");
        var actor = (PublicElement)api.Create(request with { Shape = EncounterOverlayShape.Ring, FollowActor = true, ActorId = 123 });
        Check(actor.type == 1 && actor.refActorObjectID == 123 && actor.refActorComparisonType == 2);
        var line = (PublicElement)api.Create(request with { Shape = EncounterOverlayShape.Line });
        Check(line.type == 2 && line.radius == 0f && line.offY == 6f && line.offZ == 5f);
        var label = (PublicElement)api.Create(request with { Shape = EncounterOverlayShape.Label });
        Check(label.radius == 0f && label.overlayText == "Sentinel");
        renderer.Groups["AnotherPlugin"] = ([actor], [0]);
        api.Remove(key); Check(!renderer.Groups.ContainsKey(key) && renderer.Groups.ContainsKey("AnotherPlugin"));
        var rejected = false;
        try { _ = new SplatoonElementContract(new object()); } catch (NotSupportedException) { rejected = true; }
        Check(rejected);
    }

    public sealed class PublicRenderer
    {
        public Dictionary<string, (PublicElement[] Elements, long[] Expiry)> Groups { get; } = new();
        public void AddDynamicElements(string name, PublicElement[] elements, long[] expiry) => Groups[name] = (elements, expiry);
        public void RemoveDynamicElements(string name) => Groups.Remove(name);
    }
    public sealed class PublicElement(int kind)
    {
        public string Name = string.Empty;
        public bool Enabled = false;
        public int type = kind;
        public float refX = 0f, refY = 0f, refZ = 0f, offX = 0f, offY = 0f, offZ = 0f, radius = 0f, thicc = 0f;
        public uint color = 0, overlayTextColor = 0, refActorObjectID = 0;
        public bool Filled = false;
        public string overlayText = string.Empty;
        public int refActorType = 0, refActorComparisonType = 0;
    }
}
