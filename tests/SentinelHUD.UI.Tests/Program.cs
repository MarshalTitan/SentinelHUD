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
            Test("arc paths, shields and wrapped text render at three widths/scales", TestArcRendering);
            Test("arc islands pass empty space through and preserve bar clicks", TestArcInputs);
            Test("currency compact/full layouts wrap at narrow widths", TestCurrencyLayouts);
            Console.WriteLine($"{passed}/10 native ImGui UI test groups passed.");
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

    private static void TestArcRendering()
    {
        foreach (var width in new[] { 180f, 320f, 640f })
        foreach (var scale in new[] { 0.65f, 1f, 1.75f })
        foreach (var orientation in new[] { ArcOrientation.Left, ArcOrientation.Right })
        InContext(() =>
        {
            var renderer = new ArcHudRenderer();
            var module = new PlayerModuleConfiguration
            {
                BarStyle = HudBarStyle.Arc, Width = width, ArcOrientation = orientation,
                ArcHeight = 240f, BarHeight = 34f,
            };
            for (var frame = 0; frame < 3; frame++)
            {
                ImGui.NewFrame();
                ImGui.SetNextWindowPos(new Vector2(40f, 40f));
                ImGui.SetNextWindowSize(new Vector2(width, 1200f * scale));
                ImGui.Begin("Arc rendering test", ImGuiWindowFlags.NoSavedSettings);
                ImGui.SetWindowFontScale(scale);
                var start = ImGui.GetCursorScreenPos();
                renderer.Draw(module, HudModuleKind.Player, scale, 0.8f,
                    frame == 0 ? 0f : frame == 1 ? 0.75f : 1f, 0.3f, 0.6f, true,
                    new ArcReadout("A long combatant name for wrapping", "Lv.100",
                        "120,000 / 150,000", "8,000 / 10,000", "Shield 30%", "10.0y", "80.0%", "80.0%"),
                    new Vector4(0.2f, 0.8f, 0.3f, 1f), new Vector4(0.2f, 0.4f, 1f, 1f),
                    new Vector4(0.2f, 0.6f, 1f, 1f));
                Check(ImGui.GetCursorScreenPos().Y >= start.Y + 240f * scale,
                    "Arc rendering did not reserve its full height.");
                Check(renderer.HeaderBounds.Visible, "Integrated name/level header disappeared.");
                Check(renderer.HeaderBounds.Maximum.Y <= renderer.HpTextBounds.Minimum.Y,
                    "Header overlaps the HP readout.");
                var centre = ImGui.GetWindowPos().X + ImGui.GetWindowSize().X * 0.5f;
                Check(orientation == ArcOrientation.Left
                    ? renderer.HpTextBounds.Maximum.X < centre && renderer.HpPercentageBounds.Minimum.X > centre
                    : renderer.HpPercentageBounds.Maximum.X < centre && renderer.HpTextBounds.Minimum.X > centre,
                    "Outer side values and inner side percentage are not mirrored correctly.");
                Check(renderer.HpTextBounds.Maximum.Y < renderer.MpTextBounds.Minimum.Y,
                    "HP and MP readouts overlap.");
                var rightEdge = ImGui.GetWindowPos().X + ImGui.GetWindowSize().X - ImGui.GetStyle().WindowPadding.X;
                Check(renderer.HpTextBounds.Maximum.X <= rightEdge + 1f
                    && renderer.MpTextBounds.Maximum.X <= rightEdge + 1f, "Resource readout overflows the module.");
                if (frame > 0)
                {
                    AssertTextColour(renderer.HpTextBounds, new Vector4(0.2f, 0.8f, 0.3f, 0.8f));
                    AssertTextColour(renderer.HpPercentageBounds, new Vector4(0.2f, 0.8f, 0.3f, 0.8f));
                    AssertTextColour(renderer.MpTextBounds, new Vector4(0.2f, 0.4f, 1f, 0.8f));
                    AssertTextColour(renderer.MpPercentageBounds, new Vector4(0.2f, 0.4f, 1f, 0.8f));
                    var fringe = false;
                    var band = renderer.GetHitBand(12);
                    var vertices = ImGui.GetWindowDrawList().VtxBuffer;
                    for (var i = 0; i < vertices.Size; i++)
                        if ((vertices[i].Col >> 24) == 0 && band.Contains(vertices[i].Pos)) fringe = true;
                    Check(fringe, "Smooth tube edge has no transparent antialiasing fringe.");
                }
                ImGui.End();
                ImGui.Render();
                if (frame > 0) Check(ImGui.GetDrawData().TotalVtxCount > 0, "Arc paths produced no render data.");
            }
        });
    }

    private static void TestArcInputs()
    {
        foreach (var side in new[] { ArcOrientation.Left, ArcOrientation.Right })
        InContext(() =>
        {
            var renderer = new ArcHudRenderer();
            var module = new PlayerModuleConfiguration { BarStyle = HudBarStyle.Arc, ArcOrientation = side };
            var io = ImGui.GetIO();
            var leftClicks = 0;
            var rightClicks = 0;
            var fullRectangle = false;
            var mp = true;
            for (var i = 0; i < 4; i++) Frame(new Vector2(-100f, -100f));
            var hpBand = renderer.GetHitBand(12);
            var hpPoint = (hpBand.Minimum + hpBand.Maximum) * 0.5f;
            Settle(hpPoint);
            Check(io.WantCaptureMouse, "Visible HP bar is not interactive.");
            io.AddMouseButtonEvent(0, true); Frame(hpPoint);
            io.AddMouseButtonEvent(0, false); Frame(hpPoint);
            Check(leftClicks == 1, "Visible HP bar did not deliver exactly one left click.");
            io.AddMouseButtonEvent(1, true); Frame(hpPoint);
            io.AddMouseButtonEvent(1, false); Frame(hpPoint);
            Check(rightClicks == 1, "Visible HP bar did not deliver exactly one right click.");
            var mpBand = renderer.GetHitBand(ArcHitRegions.BandsPerTrack + 12);
            var mpPoint = (mpBand.Minimum + mpBand.Maximum) * 0.5f;
            Settle(mpPoint); Check(io.WantCaptureMouse, "Visible MP bar is not interactive.");
            mp = false; Settle(mpPoint);
            Check(!io.WantCaptureMouse, "Hidden MP bar retained an input region.");
            var empty = side == ArcOrientation.Left ? new Vector2(310f, 120f) : new Vector2(50f, 120f);
            Settle(empty); Check(!io.WantCaptureMouse, "Empty module interior blocks world mouse input.");
            var footer = (renderer.HpTextBounds.Minimum + renderer.HpTextBounds.Maximum) * 0.5f;
            Settle(footer); Check(!io.WantCaptureMouse, "Resource text blocks world mouse input.");
            var percentage = (renderer.HpPercentageBounds.Minimum + renderer.HpPercentageBounds.Maximum) * 0.5f;
            Settle(percentage); Check(!io.WantCaptureMouse, "Percentage blocks world mouse input.");
            fullRectangle = true; Settle(empty);
            Check(io.WantCaptureMouse, "Full rectangle selection for Standard/edit regions changed.");

            void Settle(Vector2 point) { for (var i = 0; i < 4; i++) Frame(point); }
            void Frame(Vector2 mouse)
            {
                io.AddMousePosEvent(mouse.X, mouse.Y);
                ImGui.NewFrame();
                ImGui.SetNextWindowPos(new Vector2(40f, 40f));
                ImGui.SetNextWindowSize(new Vector2(320f, 600f));
                ImGui.Begin("Arc input visual", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration
                    | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBackground);
                renderer.Draw(module, HudModuleKind.Player, 1f, 1f, 0.8f, 0.2f, 0.5f, mp,
                    new ArcReadout("Player", "Lv.100", "58,500 / 58,500", mp ? "10,000 / 10,000" : "", "", "", "100.0%"),
                    Vector4.One, Vector4.One, Vector4.One);
                ImGui.End();
                var count = fullRectangle ? 1 : renderer.HitBandCount;
                for (var i = 0; i < count; i++)
                {
                    var band = fullRectangle ? new ArcHitBand(new Vector2(40f, 40f), new Vector2(360f, 640f))
                        : renderer.GetHitBand(i);
                    var clicked = ActorInputRegionRenderer.Draw("Arc input band " + i, band.Minimum, band.Maximum, true, false);
                    if (clicked.Left) leftClicks++;
                    if (clicked.Right) rightClicks++;
                }
                ImGui.Render();
            }
        });
    }

    private static void TestCurrencyLayouts()
    {
        foreach (var width in new[] { 64f, 120f, 180f, 480f, 640f })
        foreach (var scale in new[] { 0.75f, 1.5f })
        foreach (var full in new[] { false, true })
        foreach (var orientation in new[] { CurrencyOrientation.Horizontal, CurrencyOrientation.Vertical })
        foreach (var alignment in new[] { CurrencyAlignment.Left, CurrencyAlignment.Right })
        foreach (var showIcons in new[] { false, true })
        InContext(() =>
        {
            var renderer = new CurrencyTrackerRenderer(null);
            var config = HudConfigurationDefaults.CreateCurrencyTracker();
            config.Enabled = true; config.ShowNames = full; config.Orientation = orientation;
            config.Alignment = alignment; config.ShowIcons = showIcons;
            CurrencyBalance[] balances =
            [
                new(new CurrencyDefinition(1, "Gil", 0, null, false), 123456789, null),
                new(new CurrencyDefinition(90001, "Allagan Tomestone of Mnemonics", 0, 2000, true), 1989, 2000),
                new(new CurrencyDefinition(90002, "Bicolor Gemstones", 0, 1500, false), 782, 1500),
                new(new CurrencyDefinition(90003, "Wolf Marks", 0, 20000, false), 7825, 20000),
            ];
            for (var frame = 0; frame < 3; frame++)
            {
                ImGui.NewFrame();
                ImGui.SetNextWindowPos(new Vector2(40f, 40f));
                ImGui.SetNextWindowSize(new Vector2(width, 2000f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                ImGui.Begin("Currency layout", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoInputs);
                ImGui.SetWindowFontScale(scale);
                var startVertex = ImGui.GetWindowDrawList().VtxBuffer.Size;
                renderer.Draw(config, balances, scale, 0.8f, false);
                Check(renderer.EntryCount == 4, "Selected currencies disappeared.");
                Check(renderer.RowCount is >= 1 and <= 4, "Unexpected currency row count.");
                if (orientation == CurrencyOrientation.Vertical) Check(renderer.RowCount == 4, "Vertical orientation did not stack entries.");
                var edge = ImGui.GetWindowPos().X + ImGui.GetWindowSize().X - ImGui.GetStyle().WindowPadding.X;
                Check(ImGui.GetItemRectMax().X <= edge + 1f, "Currency entries overflow the module width.");
                var vertices = ImGui.GetWindowDrawList().VtxBuffer;
                var lastX = float.MinValue;
                for (var vertex = startVertex; vertex < vertices.Size; vertex++)
                {
                    lastX = Math.Max(lastX, vertices[vertex].Pos.X);
                    Check(vertices[vertex].Pos.X <= edge + 2f, "Currency glyphs extend past the right edge.");
                }
                if (alignment == CurrencyAlignment.Right)
                    Check(lastX >= edge - 4f * scale, "Right alignment leaves an unused gutter.");
                ImGui.End(); ImGui.PopStyleVar(); ImGui.Render();
            }
        });
    }

    private static void AssertTextColour(ArcTextBounds bounds, Vector4 expected)
    {
        var colour = ImGui.ColorConvertFloat4ToU32(expected);
        var vertices = ImGui.GetWindowDrawList().VtxBuffer;
        var count = 0;
        for (var i = 0; i < vertices.Size; i++)
        {
            var vertex = vertices[i];
            if (vertex.Col == colour && vertex.Pos.X >= bounds.Minimum.X && vertex.Pos.X <= bounds.Maximum.X
                && vertex.Pos.Y >= bounds.Minimum.Y && vertex.Pos.Y <= bounds.Maximum.Y) count++;
        }
        Check(count >= 4, "Resource glyph vertices do not use the expected resource colour/opacity.");
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
                    ImGui.SetNextWindowPos(Program.Position, ImGuiCond.FirstUseEver);
                    if (Size is { } size) ImGui.SetNextWindowSize(size * scale, SizeCondition);
                    if (Collapsed is { } collapsed) ImGui.SetNextWindowCollapsed(collapsed, CollapsedCondition);
                    if (SizeConstraints is { } constraints)
                        ImGui.SetNextWindowSizeConstraints(constraints.MinimumSize * scale, constraints.MaximumSize * scale);
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
