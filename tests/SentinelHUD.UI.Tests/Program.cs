using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using SentinelCore.UI;
using SentinelHUD.Core;
using SentinelHUD.UI;

internal static class Program
{
    private static int passed;
    private static readonly Vector2 Position = new(145f, 95f);

    public static int Main()
    {
        var dalamud = Environment.GetEnvironmentVariable("DALAMUD_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher", "addon", "Hooks", "dev");
        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            var path = Path.Combine(dalamud, name.Name + ".dll");
            return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
        };
        try
        {
            Test("wide/medium/narrow Modern minimize geometry at three UI scales", TestGeometry);
            Test("Core pointer minimize/expand/close controls", TestPointerControls);
            Test("Core keyboard restore control", () => TestNavigation(ImGuiKey.Space));
            Test("Core controller restore control", () => TestNavigation(ImGuiKey.GamepadFaceDown));
            Test("minimized reload and command reopening restore expanded dimensions", TestReload);
            Test("minimized Core header dragging retains expanded size", TestDragging);
            Test("Classic native collapse and theme switching retain geometry", TestClassic);
            Console.WriteLine($"{passed}/7 native ImGui UI test groups passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Test(string name, Action run)
    {
        run();
        passed++;
        Console.WriteLine("PASS: " + name);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static unsafe void InContext(Action run)
    {
        var context = ImGui.CreateContext();
        try
        {
            var io = ImGui.GetIO();
            io.IniFilename = null;
            io.LogFilename = null;
            io.DisplaySize = new Vector2(3000f, 2400f);
            io.DeltaTime = 1f / 60f;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;
            io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
            io.Fonts.AddFontDefault();
            Check(io.Fonts.Build(), "Font atlas did not build.");
            run();
        }
        finally { ImGui.DestroyContext(context); }
    }

    private static unsafe ImGuiWindowPtr FindWindow(string name, string? child = null)
    {
        var windows = ImGui.GetCurrentContext().Windows;
        for (var index = 0; index < windows.Size; index++)
        {
            var window = windows[index];
            var title = Marshal.PtrToStringUTF8((nint)window.Name)!;
            if (child is null ? title == name : title.StartsWith(name + "/", StringComparison.Ordinal) && title.Contains(child))
                return window;
        }
        throw new InvalidOperationException("Missing native window: " + name + "/" + child);
    }

    private static unsafe void TestGeometry()
    {
        foreach (var scale in new[] { 1f, 1.25f, 1.5f })
        foreach (var size in new[] { new Vector2(720f, 560f), new Vector2(920f, 720f), new Vector2(1440f, 900f) })
            InContext(() =>
            {
                using var window = new Fixture(size, scale);
                window.Settle();
                var native = FindWindow(window.WindowName);
                var before = native.Pos;
                window.ToggleMinimize();
                window.Settle();
                Check(native.Size == new Vector2(size.X, 56f) * scale, "Minimized size does not match the Core header.");
                Check(native.Pos == before && !native.Collapsed, "Minimize moved or natively collapsed the window.");
                Check(SentinelModernWindowChrome.HasSingleCustomHeader(native.Flags), "Minimize lost the Core custom chrome.");
                var header = FindWindow(window.WindowName, "##Modern2Header");
                Check(header.Size.Y == 56f * scale && !header.ScrollbarX && !header.ScrollbarY, "Header clipped or acquired scrollbars.");
                var calls = window.BodyCalls;
                window.Settle();
                Check(window.BodyCalls == calls && window.HeaderCalls > 0, "Settings rendered while minimized, or header disappeared.");
                window.ToggleMinimize();
                window.Settle();
                Check(native.Size == size * scale && native.Pos == before, "Restore lost expanded geometry.");
                Check(window.Size is null, "One-shot size request was left active.");
            });
    }

    private static unsafe void TestPointerControls() => InContext(() =>
    {
        using var window = new Fixture(new Vector2(1120f, 860f));
        window.Settle();
        for (var cycle = 0; cycle < 5; cycle++)
        {
            Click(window, window.MinimizePoint());
            window.Settle();
            Check(window.State.IsMinimized, "Core minimize did not activate.");
            Click(window, window.MinimizePoint());
            window.Settle();
            Check(!window.State.IsMinimized && FindWindow(window.WindowName).Size == new Vector2(1120f, 860f), "Core Expand did not restore.");
        }
        Click(window, window.MinimizePoint());
        window.Settle();
        var header = FindWindow(window.WindowName, "##Modern2Header");
        Click(window, header.Pos + new Vector2(header.Size.X - 29f, header.Size.Y / 2f));
        Check(!window.IsOpen, "Core close did not work while minimized.");
    });

    private static unsafe void TestNavigation(ImGuiKey key) => InContext(() =>
    {
        using var window = new Fixture(new Vector2(920f, 720f));
        window.Settle();
        Click(window, window.MinimizePoint());
        window.Settle();
        Check(window.LastCollapseId != 0, "Core collapse button ID was not captured.");
        ImGui.GetIO().AddMousePosEvent(-100f, -100f);
        window.Frame();
        var context = ImGui.GetCurrentContext();
        context.NavWindow = FindWindow(window.WindowName, "##Modern2Header");
        context.NavId = window.LastCollapseId;
        context.NavDisableHighlight = false;
        context.NavDisableMouseHover = true;
        window.Frame();
        ImGui.GetIO().AddKeyEvent(key, true);
        window.Frame();
        ImGui.GetIO().AddKeyEvent(key, false);
        window.Settle();
        Check(!window.State.IsMinimized && FindWindow(window.WindowName).Size == new Vector2(920f, 720f), "Navigation did not restore the window.");
    });

    private static unsafe void TestReload() => InContext(() =>
    {
        using var original = new Fixture(new Vector2(1120f, 860f), 1.25f);
        original.Settle();
        original.ToggleMinimize();
        original.Settle();
        var saved = new ModernWindowConfiguration();
        original.State.SaveTo(saved);
        var reloaded = JsonSerializer.Deserialize<ModernWindowConfiguration>(JsonSerializer.Serialize(saved))!;
        using var restored = new Fixture(new Vector2(920f, 720f), 1.25f, reloaded);
        restored.Settle();
        var native = FindWindow(restored.WindowName);
        Check(native.Size == new Vector2(1120f, 56f) * 1.25f && native.Pos == Position, "Reload lost minimized placement.");
        restored.State.Expand(); // Same state operation used by /shud and Open Config.
        restored.Settle();
        Check(native.Size == new Vector2(1120f, 860f) * 1.25f, "Reopening restored the header size instead of the expanded size.");
    });

    private static unsafe void TestDragging() => InContext(() =>
    {
        using var window = new Fixture(new Vector2(1120f, 860f));
        window.Settle();
        window.ToggleMinimize();
        window.Settle();
        var header = FindWindow(window.WindowName, "##Modern2Header");
        var start = header.Pos + new Vector2(650f, header.Size.Y / 2f);
        var io = ImGui.GetIO();
        io.AddMousePosEvent(start.X, start.Y);
        window.Frame();
        io.AddMouseButtonEvent(0, true);
        window.Frame();
        io.AddMousePosEvent(start.X + 40f, start.Y + 25f);
        window.Frame();
        io.AddMouseButtonEvent(0, false);
        window.Settle();
        var moved = FindWindow(window.WindowName).Pos;
        Check(moved == Position + new Vector2(40f, 25f), "The minimized Core header did not drag.");
        window.ToggleMinimize();
        window.Settle();
        Check(FindWindow(window.WindowName).Pos == moved && FindWindow(window.WindowName).Size == new Vector2(1120f, 860f), "Restore lost dragged placement or expanded size.");
    });

    private static unsafe void TestClassic() => InContext(() =>
    {
        using var window = new Fixture(new Vector2(920f, 720f));
        window.Modern = false;
        window.Settle();
        ImGui.SetWindowCollapsed(window.WindowName, true);
        window.Frame();
        Check(FindWindow(window.WindowName).Collapsed, "Classic native collapse was disabled.");
        window.State.Expand();
        window.Settle();
        Check(!FindWindow(window.WindowName).Collapsed, "Classic Open Config did not expand.");
        window.Modern = true;
        window.Settle();
        window.ToggleMinimize();
        window.Settle();
        window.Modern = false;
        window.Settle();
        var native = FindWindow(window.WindowName);
        Check(native.Size == new Vector2(920f, 720f) && (native.Flags & ImGuiWindowFlags.NoTitleBar) == 0,
            "Switching to Classic retained the minimized height or custom chrome.");
    });

    private static void Click(Fixture window, Vector2 point)
    {
        var io = ImGui.GetIO();
        io.AddMousePosEvent(point.X, point.Y);
        window.Frame();
        io.AddMouseButtonEvent(0, true);
        window.Frame();
        io.AddMouseButtonEvent(0, false);
        window.Frame();
    }

    private sealed class Fixture : Window, IDisposable
    {
        private readonly SentinelModernStyleScope style = new();
        private readonly SentinelModernAppShellState shell = new();
        private readonly float scale;
        public SentinelHudModernWindowState State { get; }
        public bool Modern { get; set; } = true;
        public int BodyCalls { get; private set; }
        public int HeaderCalls { get; private set; }
        public uint LastCollapseId { get; private set; }

        public Fixture(Vector2 size, float scale = 1f, ModernWindowConfiguration? saved = null) : base("HUD chrome test##HUD")
        {
            this.scale = scale;
            ImGui.GetIO().FontGlobalScale = scale;
            State = new SentinelHudModernWindowState(saved, SentinelModernAppLayoutOptions.Default.HeaderHeight);
            Size = size;
            SizeCondition = ImGuiCond.FirstUseEver;
            IsOpen = true;
        }

        public override void PreDraw()
        {
            if (!Modern && State.IsMinimized) State.Expand();
            ModernWindowPresentation.Prepare(this, State, Modern, ImGuiWindowFlags.None, new Vector2(720f, 560f));
            if (Modern) style.PushAppShell(scale);
        }

        public override void Draw()
        {
            if (!Modern) { ImGui.TextUnformatted("Classic"); return; }
            ModernWindowPresentation.DrawShell(new SentinelModernAppShellOptions("Test", "Sentinel HUD", "General")
            {
                Scale = scale, ReducedMotion = true, EnableWindowDragging = true,
                ContextLabel = "General", DrawPluginIcon = _ => HeaderCalls++,
                RequestCollapse = ToggleMinimize, RequestClose = () => IsOpen = false,
                Status = new SentinelModernStatusPillOptions("HUD LOCKED", SentinelModernPillTone.Ready),
            }, State, shell, [new SentinelModernNavItem("General", "G", "General")],
                _ => { }, () => BodyCalls++, () => BodyCalls++, () => BodyCalls++);
        }

        public void Frame()
        {
            ImGui.NewFrame();
            if (IsOpen)
            {
                PreDraw();
                try
                {
                    // Match Dalamud WindowHost's public property application order and scaling.
                    ImGui.SetNextWindowPos(Position, ImGuiCond.FirstUseEver);
                    if (Size is { } size) ImGui.SetNextWindowSize(size * scale, SizeCondition);
                    if (Collapsed is { } collapsed) ImGui.SetNextWindowCollapsed(collapsed, CollapsedCondition);
                    if (SizeConstraints is { } constraints)
                        ImGui.SetNextWindowSizeConstraints((constraints.MinimumSize ?? Vector2.Zero) * scale,
                            (constraints.MaximumSize ?? new Vector2(float.MaxValue)) * scale);
                    var open = IsOpen;
                    if (ImGui.Begin(WindowName, ref open, Flags)) Draw();
                    ImGui.End();
                    IsOpen &= open;
                }
                finally { style.Pop(); }
            }
            ImGui.Render();
        }

        public void Settle() { for (var index = 0; index < 3; index++) Frame(); }

        public unsafe void ToggleMinimize()
        {
            LastCollapseId = ImGui.GetCurrentContext().LastItemData.ID;
            if (State.IsMinimized) State.Expand();
            else State.Minimize(SentinelModernAppLayoutOptions.Default.HeaderHeight);
        }

        public unsafe Vector2 MinimizePoint()
        {
            var header = FindWindow(WindowName, "##Modern2Header");
            return header.Pos + new Vector2(header.Size.X - 65f * scale, header.Size.Y / 2f);
        }

        public void Dispose() => style.Dispose();
    }
}
