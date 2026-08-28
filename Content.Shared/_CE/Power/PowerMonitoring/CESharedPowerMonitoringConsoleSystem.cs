using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Content.Shared._CE.Power.PowerMonitoring;

/// <summary>
/// CE fork of <c>Content.Shared.Power.SharedPowerMonitoringConsoleSystem</c>, adapted for the
/// multi z-level <c>CEZLevelsNavMapControl</c>. Cable chunks are tracked per grid (one grid per
/// z-level) instead of for a single grid.
/// </summary>
[UsedImplicitly]
public abstract class CESharedPowerMonitoringConsoleSystem : EntitySystem
{
    // Chunk size is limited as we require ChunkSize^2 <= 32 (number of bits in an int)
    public const int ChunkSize = 5;

    /// <summary>
    /// Converts the chunk's tile into a bitflag for the slot.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFlag(Vector2i relativeTile)
    {
        return 1 << (relativeTile.X * ChunkSize + relativeTile.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetTileFromIndex(int index)
    {
        var x = index / ChunkSize;
        var y = index % ChunkSize;
        return new Vector2i(x, y);
    }
}
