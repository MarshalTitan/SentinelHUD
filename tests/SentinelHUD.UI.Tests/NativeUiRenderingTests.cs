using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelHUD.Core;
using SentinelHUD.UI;
using SentinelCore.Configuration;

internal static class NativeUiRenderingTests
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static unsafe void Context(Action test)
    {
        var context = ImGui.CreateContext();
        try
        {
            var io = ImGui.GetIO(); io.IniFilename = null; io.LogFilename = null;
            io.DisplaySize = new Vector2(1000f, 800f); io.DeltaTime = 1f / 60f;
            io.Fonts.AddFontDefault(); Check(io.Fonts.Build(), "Font atlas failed");
            test();
        }
        finally { ImGui.DestroyContext(context); }
    }

    public static void Commands()
    {
        foreach (var full in new[] { false, true })
        Context(() =>
        {
            var clipper = new NativeUiDrawClipper();
            clipper.Mask.Add(full ? new(0f, 0f, 1000f, 800f) : new(240f, 140f, 350f, 330f));
            if (!full) clipper.Mask.Add(new(300f, 200f, 420f, 360f));
            for (var frame = 0; frame < 3; frame++)
            {
                ImGui.NewFrame(); ImGui.SetNextWindowPos(new(100f, 100f)); ImGui.SetNextWindowSize(new(400f, 300f));
                ImGui.Begin("HUD clipping", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings);
                var list = ImGui.GetWindowDrawList();
                list.AddRectFilled(new(110f, 110f), new(490f, 390f), 0xFF55AA77, 12f);
                list.AddText(new(120f, 145f), 0xFFFFFFFF, "Health 205,960 100.0%");
                var vertices = Enumerable.Range(0, list.VtxBuffer.Size).Select(i => list.VtxBuffer[i]).ToArray();
                var indices = Enumerable.Range(0, list.IdxBuffer.Size).Select(i => list.IdxBuffer[i]).ToArray();
                ImGui.End();
                Check(clipper.Apply(list), "Expected overlap was not clipped");
                Check(list.IdxBuffer.Size == indices.Length && list.VtxBuffer.Size == vertices.Length, "Clipping duplicated geometry");
                for (var i = 0; i < indices.Length; i++) Check(indices[i] == list.IdxBuffer[i], "Index changed");
                for (var i = 0; i < vertices.Length; i++)
                    Check(vertices[i].Pos == list.VtxBuffer[i].Pos && vertices[i].Col == list.VtxBuffer[i].Col, "Vertex changed");
                var visible = 0;
                foreach (var cmd in list.CmdBuffer)
                {
                    if (cmd.ElemCount == 0) continue;
                    var r = new ScreenRectangle(cmd.ClipRect.X, cmd.ClipRect.Y, cmd.ClipRect.Z, cmd.ClipRect.W);
                    Check(!clipper.Mask.Intersects(r), "Visible command crosses native window");
                    Check((ulong)cmd.IdxOffset + cmd.ElemCount <= (ulong)indices.Length, "Invalid draw range");
                    visible++;
                }
                Check(full ? visible == 0 : visible > 1, "Partial/full occlusion lost visible regions");
                ImGui.Render();
            }
        });
    }

    public static void SharedLists() => Context(() =>
    {
        var clipper = new NativeUiDrawClipper(); clipper.Mask.Add(new(200f, 100f, 350f, 300f));
        ImGui.NewFrame();
        var list = ImGui.GetForegroundDrawList();
        list.AddRectFilled(new(20f), new(80f), 0xFFFFFFFF);
        var firstIndex = (uint)list.IdxBuffer.Size;
        var original = list.CmdBuffer[0];
        list.AddRectFilled(new(150f, 120f), new(400f, 350f), 0xFF2277AA);
        Check(clipper.Apply(list, firstIndex), "Shared drawing not clipped");
        Check(list.CmdBuffer[0].ElemCount == firstIndex && list.CmdBuffer[0].ClipRect == original.ClipRect,
            "Earlier foreign geometry was clipped");
        var afterHud = (uint)list.IdxBuffer.Size;
        list.AddRectFilled(new(450f, 100f), new(600f, 250f), 0xFFFFFFFF);
        var suffix = list.CmdBuffer[list.CmdBuffer.Size - 1];
        Check(suffix.IdxOffset == afterHud && suffix.ElemCount > 0 && suffix.ClipRect == original.ClipRect,
            "Later configuration/foreign drawing inherited the HUD mask");
        ImGui.Render();
    });

    public static void Editor() => Context(() =>
    {
        using var configuration = new ConfigurationCoordinator<SentinelHUD.Configuration>(new MemoryStore(),
            () => new() { Locked = false }, value => { HudConfigurationMigrator.Normalize(value); return value; }, saveAfterLoad: false);
        var editor = new HudEditorInteractionRenderer(configuration);
        var mask = new NativeUiOcclusionMask(); mask.Add(new(150f, 100f, 250f, 220f));
        var io = ImGui.GetIO();
        Settle(new(200f, 150f)); Check(!io.WantCaptureMouse, "Editor blocks native window");
        var original = configuration.Current.Player.Layout.AnchorX;
        io.AddMouseButtonEvent(0, true); Frame(new(200f, 150f)); Frame(new(220f, 160f));
        io.AddMouseButtonEvent(0, false); Frame(new(220f, 160f));
        Check(configuration.Current.Player.Layout.AnchorX == original, "Occluded editor drag moved module");
        Settle(new(60f, 60f)); Check(io.WantCaptureMouse, "Visible editor is not draggable");
        io.AddMouseButtonEvent(0, true); Frame(new(60f, 60f)); Frame(new(80f, 80f));
        Check(configuration.Current.Player.Layout.AnchorX != original, "Visible editor drag failed");
        var moved = configuration.Current.Player.Layout.AnchorX;
        Frame(new(200f, 150f)); Frame(new(300f, 250f));
        Check(configuration.Current.Player.Layout.AnchorX == moved, "Editor drag resumed through native window");
        io.AddMouseButtonEvent(0, false); Frame(new(300f, 250f));
        var width = configuration.Current.Player.Width;
        Settle(new(355f, 60f)); io.AddMouseButtonEvent(0, true); Frame(new(355f, 60f)); Frame(new(375f, 60f));
        io.AddMouseButtonEvent(0, false); Frame(new(375f, 60f));
        Check(configuration.Current.Player.Width > width, "Visible original editor edge did not resize");
        mask.Clear(); Settle(new(200f, 150f)); Check(io.WantCaptureMouse, "Editor retained native window hole");

        void Settle(Vector2 p) { for (var i = 0; i < 4; i++) Frame(p); }
        void Frame(Vector2 p)
        {
            io.AddMousePosEvent(p.X, p.Y); ImGui.NewFrame(); editor.BeginFrame();
            editor.Register(HudModuleKind.Player, new(40f, 40f), new(360f, 280f), configuration.Current.Player.Width, 22f, 2);
            editor.Draw(mask); ImGui.Render();
        }
    });
    private sealed class MemoryStore : IConfigurationStore<SentinelHUD.Configuration>
    {
        public SentinelHUD.Configuration? Load() => null;
        public void Save(SentinelHUD.Configuration configuration) { }
    }

    public static void Input() => Context(() =>
    {
        var mask = new NativeUiOcclusionMask(); mask.Add(new(150f, 100f, 250f, 220f));
        var io = ImGui.GetIO(); var clicks = 0; var rightClicks = 0;
        var fractional = new NativeUiOcclusionMask(); fractional.Add(new(40.25f, 40f, 360f, 280f));
        var partition = new NativeUiInputPartition();
        Check(partition.Split(new(40f, 40f), new(360f, 280f), fractional).Count == 0,
            "Subpixel input fragment can expand back into native window");
        Settle(new(60f, 60f)); Check(io.WantCaptureMouse, "Visible actor region is not interactive");
        io.AddMouseButtonEvent(0, true); Frame(new(60f, 60f));
        io.AddMouseButtonEvent(0, false); Frame(new(60f, 60f));
        Check(clicks == 1, "Uncovered actor click failed");
        Settle(new(200f, 150f)); Check(!io.WantCaptureMouse, "Native window is blocked by invisible HUD rectangle");
        io.AddMouseButtonEvent(0, true); Frame(new(200f, 150f));
        io.AddMouseButtonEvent(0, false); Frame(new(200f, 150f));
        io.AddMouseButtonEvent(1, true); Frame(new(200f, 150f));
        io.AddMouseButtonEvent(1, false); Frame(new(200f, 150f));
        Check(clicks == 1 && rightClicks == 0, "Occluded region delivered actor action");
        mask.Clear(); Settle(new(200f, 150f)); Check(io.WantCaptureMouse, "Closing native window left stale input hole");
        io.AddMouseButtonEvent(1, true); Frame(new(200f, 150f));
        io.AddMouseButtonEvent(1, false); Frame(new(200f, 150f));
        Check(rightClicks == 1, "Native actor context interaction did not return");

        void Settle(Vector2 point) { for (var i = 0; i < 4; i++) Frame(point); }
        void Frame(Vector2 point)
        {
            io.AddMousePosEvent(point.X, point.Y); ImGui.NewFrame();
            var clicked = ActorInputRegionRenderer.Draw("Occluded interaction", new(40f, 40f), new(360f, 280f), true, false, mask);
            if (clicked.Left) clicks++; if (clicked.Right) rightClicks++;
            ImGui.Render();
        }
    });
}
