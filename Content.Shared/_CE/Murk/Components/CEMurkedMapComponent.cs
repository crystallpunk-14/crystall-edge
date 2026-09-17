using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Sets the base murk value on the map, which can be modified by various sources.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEMurkedMapComponent : Component
{
    /// <summary>
    /// Base murk of this map. 0 means no murk at all, 1 means murk at full strength.
    /// Expected to change at runtime (round events, admin commands) - do not cache it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Intensity = 1;

    /// <summary>
    /// Brightest tint the murk drifts to. The darkest point is always pure black.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color MurkColor = new(0.025f, 0.032f, 0.054f);

    /// <summary>
    /// Client-side render value chasing <see cref="Intensity"/>. Always starts at zero, so murk
    /// rolls in smoothly even when the component is added to an already running map.
    /// </summary>
    [NonSerialized]
    public float LerpedIntensity;

    /// <summary>
    ///     Maximum number of murk sources that can be shown on screen at a time.
    ///     If this value is changed, the shader itself also needs to be updated.
    /// </summary>
    public const int MaxCount = 64;

    /// <summary>
    /// Smoothed state of every source affecting this map, already projected onto its z-level.
    /// Entries outlive the source entity so that removing a lighthouse fades its sphere out
    /// instead of blinking it away.
    /// </summary>
    [NonSerialized]
    public readonly Dictionary<EntityUid, MurkEntry> MurkBuffer = new();

    [NonSerialized]
    public readonly HashSet<EntityUid> Seen = [];

    // Flattened copy of the buffer handed to the shader.
    [NonSerialized]
    public readonly Vector2[] Positions = new Vector2[MaxCount];

    [NonSerialized]
    public readonly float[] Radii = new float[MaxCount];

    [NonSerialized]
    public readonly float[] Strengths = new float[MaxCount];

    [NonSerialized]
    public int Count;

    public sealed class MurkEntry
    {
        public Vector2 Position;
        public float Radius;
        public float Strength;
    }
}
