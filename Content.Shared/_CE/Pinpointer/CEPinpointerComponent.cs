using System.Numerics;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Pinpointer;

/// <summary>
/// Compass-like item whose needle points to the closest whitelisted entity in the same z-network,
/// ignoring the z-level difference. With no reachable target the needle wanders aimlessly.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CEPinpointerComponent : Component
{
    /// <summary>
    /// Entities the needle can point to. Must contain at least one component - it is used to enumerate candidates.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    /// <summary>
    /// Closer than this the needle doesn't know where to point.
    /// </summary>
    [DataField]
    public float ReachedDistance = 1f;

    /// <summary>
    /// Arrow angle changes smaller than this (radians) aren't networked.
    /// </summary>
    [DataField]
    public double Precision = 0.05;

    [DataField]
    public TimeSpan SearchInterval = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSearch = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables, AutoNetworkedField]
    public bool HasDirection;

    /// <summary>
    /// World angle from the pinpointer to the target.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Angle ArrowAngle;

    /// <summary>
    /// Needle rotation point relative to the sprite center, in world units (32 px = 1), Y up.
    /// For pixel (x, y) of a 32x32 texture: ((x + 0.5 - 16) / 32, (16 - y - 0.5) / 32).
    /// The needle sprite itself must point south.
    /// </summary>
    [DataField]
    public Vector2 NeedlePivot = Vector2.Zero;

    /// <summary>
    /// How fast the needle catches up with the target direction. Higher is snappier.
    /// </summary>
    [DataField]
    public float NeedleStiffness = 6f;

    [DataField]
    public float WanderSpeedMin = 2f;

    [DataField]
    public float WanderSpeedMax = 9f;

    /// <summary>
    /// Max rotation of a single wander swing, radians. Above 2π the needle can do full spins.
    /// </summary>
    [DataField]
    public float WanderMaxSwing = 8f;

    [DataField]
    public float WanderMaxPause = 0.5f;

    // Client-side visual state, not saved or networked

    [ViewVariables]
    public Angle NeedleAngle;

    [ViewVariables]
    public float WanderRemaining;

    [ViewVariables]
    public float WanderSpeed;

    [ViewVariables]
    public TimeSpan NextWander;
}

[Serializable, NetSerializable]
public enum CEPinpointerLayers : byte
{
    Needle,
}
