using System.Numerics;
using System.Runtime.InteropServices;

namespace SentinelHUD.Services;

/// <summary>Reviewed API-15 client ABI facts; no foreign source/assembly is embedded.</summary>
public static unsafe class NativeOmenMemory
{
    public const int DataSize = 0x1E0;
    public const int InstanceOffset = 0x1B8;
    public const int ColourOffset = 0xA0;

    public static bool TryGetInstance(nint handle, out nint instance)
    {
        instance = 0;
        if (!Readable(handle, DataSize, false)) return false;
        instance = *(nint*)((byte*)handle + InstanceOffset);
        return Readable(instance, ColourOffset + sizeof(Vector4), true);
    }

    public static Vector4 Read(nint instance) => *(Vector4*)((byte*)instance + ColourOffset);
    public static void Write(nint instance, Vector4 value) => *(Vector4*)((byte*)instance + ColourOffset) = value;

    private static bool Readable(nint address, int size, bool writable)
    {
        if (address <= 0 || VirtualQuery(address, out var memory, (nuint)sizeof(MemoryInfo)) == 0
            || memory.State != 0x1000 || (memory.Protect & 0x101) != 0) return false;
        var protection = memory.Protect & 0xFF;
        if (protection is not (0x02 or 0x04 or 0x08 or 0x20 or 0x40 or 0x80)) return false;
        if (writable && protection is not (0x04 or 0x08 or 0x40 or 0x80)) return false;
        return (nuint)address + (uint)size <= (nuint)memory.Base + memory.Size;
    }

    [DllImport("kernel32.dll")] private static extern nuint VirtualQuery(nint address, out MemoryInfo info, nuint length);
    [StructLayout(LayoutKind.Sequential)] private struct MemoryInfo
    {
        public nint Base, AllocationBase;
        public uint AllocationProtect;
        public nuint Size;
        public uint State, Protect, Type;
    }
}
