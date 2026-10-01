using Robust.Shared.Serialization;

namespace Content.Shared._CE.EnergyScanner;

/// <summary>
/// Appearance data the server writes onto pipes for the energy scanner overlay.
/// </summary>
[Serializable, NetSerializable]
public enum CEEnergyScannerVisuals : byte
{
    /// <summary>
    /// bool: any node of the pipe belongs to a network with an active energy source.
    /// </summary>
    Powered,

    /// <summary>
    /// bool: the pipe is a big (HV) pipe.
    /// </summary>
    Large,

    /// <summary>
    /// <see cref="CEPipeVerticalDirection"/>: z-level connections of the pipe.
    /// </summary>
    Vertical,
}

[Flags, Serializable, NetSerializable]
public enum CEPipeVerticalDirection : byte
{
    None = 0,
    Up = 1 << 0,
    Down = 1 << 1,
}
