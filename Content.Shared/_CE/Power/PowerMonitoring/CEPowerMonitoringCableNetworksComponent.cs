using Content.Shared.Power;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Power.PowerMonitoring;

/// <summary>
/// CE fork of <c>Content.Shared.Power.PowerMonitoringCableNetworksComponent</c>, adapted for
/// multiple z-levels: cable chunks are stored per grid (one grid per z-level) rather than for a
/// single grid. The client resolves each grid's z-depth via <c>CEZMapComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CESharedPowerMonitoringConsoleSystem))]
public sealed partial class CEPowerMonitoringCableNetworksComponent : Component
{
    /// <summary>
    /// Every nav-map chunk that contains anchored power cables, keyed by grid then by chunk origin.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<NetEntity, Dictionary<Vector2i, PowerCableChunk>> AllChunks = new();

    /// <summary>
    /// The chunks that contain cables directly connected to the console's current focus, keyed by grid.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<NetEntity, Dictionary<Vector2i, PowerCableChunk>> FocusChunks = new();

    /// <summary>
    /// Severed cable edges (from <c>CECableCutComponent</c> entities, e.g. isolators), keyed by grid.
    /// The client drops these edges when merging cable segments so the map shows the break.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<NetEntity, HashSet<CECableCut>> Cuts = new();
}

/// <summary>An undirected cable edge between two adjacent grid tiles, in canonical order.</summary>
[Serializable, NetSerializable]
public struct CECableCut : IEquatable<CECableCut>
{
    public Vector2i A;
    public Vector2i B;

    public CECableCut(Vector2i t1, Vector2i t2)
    {
        if (t1.Y < t2.Y || (t1.Y == t2.Y && t1.X <= t2.X))
        {
            A = t1;
            B = t2;
        }
        else
        {
            A = t2;
            B = t1;
        }
    }

    public bool Equals(CECableCut other) => A == other.A && B == other.B;
    public override bool Equals(object? obj) => obj is CECableCut other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(A, B);
}
