using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelHUD.Core;

namespace SentinelHUD.UI;

/// <summary>
/// Reuses the existing vertices/indices/textures and splits only GPU scissors. No module redraw,
/// layout replay, geometry hook, native UI mutation or texture capture is needed.
/// </summary>
public sealed class NativeUiDrawClipper
{
    private readonly List<ImDrawCmd> commands = new(64);
    private readonly List<ScreenRectangle> pieces = new(32);
    private readonly List<ScreenRectangle> scratch = new(32);
    private ScreenRectangle lastClip;
    private bool cached;
    public NativeUiOcclusionMask Mask { get; set; } = new();

    public unsafe bool Apply(ImDrawListPtr drawList, uint firstIndex = 0)
    {
        if (Mask.Blockers.Count == 0 || drawList.IsNull || drawList.IdxBuffer.Size <= firstIndex) return false;
        commands.Clear(); cached = false;
        var changed = false;
        var count = drawList.CmdBuffer.Size;
        for (var i = 0; i < count; i++)
        {
            var command = drawList.CmdBuffer[i];
            if (command.ElemCount == 0 || command.UserCallback is not null
                || (ulong)command.IdxOffset + command.ElemCount <= firstIndex)
            { commands.Add(command); continue; }
            var rectangle = new ScreenRectangle(command.ClipRect.X, command.ClipRect.Y,
                command.ClipRect.Z, command.ClipRect.W);
            if (!Mask.Intersects(rectangle)) { commands.Add(command); continue; }
            // Shared background/foreground lists can start with another plugin's geometry in the
            // same command. Preserve that index prefix exactly, including its original scissor.
            if (command.IdxOffset < firstIndex)
            {
                var prefix = command; prefix.ElemCount = firstIndex - command.IdxOffset;
                commands.Add(prefix); command.IdxOffset = firstIndex; command.ElemCount -= prefix.ElemCount;
            }
            if (!cached || rectangle != lastClip)
            { Mask.Clip(rectangle, pieces, scratch); lastClip = rectangle; cached = true; }
            changed = true;
            foreach (var piece in pieces)
            {
                var fragment = command;
                fragment.ClipRect = new Vector4(piece.Left, piece.Top, piece.Right, piece.Bottom);
                commands.Add(fragment);
            }
        }
        if (!changed) return false;
        // ImVector's supported allocator keeps capacity between frames. Do not retain buffer pointers
        // across resize, change offsets, or duplicate index/vertex data.
        drawList.CmdBuffer.Resize(commands.Count);
        for (var i = 0; i < commands.Count; i++) drawList.CmdBuffer[i] = commands[i];
        // Later configuration/other-plugin drawing must start with the original current state.
        drawList.AddDrawCmd();
        return true;
    }
}
