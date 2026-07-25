using Content.Shared._CE.Press.Systems;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Press.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(CESharedPressSystem))]
public sealed partial class CEPressComponent : Component
{
    /// <summary>
    /// Current stage of the press's automatic crushing cycle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CEPressState State = CEPressState.Idle;

    /// <summary>
    /// When the current Preparing/Recovering stage finishes.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan StateEndTime;

    /// <summary>
    /// How long the press stays in the Preparing stage before crushing.
    /// </summary>
    [DataField]
    public TimeSpan PreparingDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long the press stays in the Recovering stage after crushing.
    /// </summary>
    [DataField]
    public TimeSpan RecoveringDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Damage applied directly to scanned entities when no CEPressTargetComponent is found on the tile.
    /// </summary>
    [DataField]
    public DamageSpecifier CrushDamage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 60 },
        },
    };

    /// <summary>
    /// Key of the sprite layer whose vertical offset is animated client-side to follow the
    /// press's crushing cycle.
    /// </summary>
    [DataField]
    public string BlockLayerKey = "block";

    /// <summary>
    /// Client-side vertical offset of the "block" sprite layer while Idle.
    /// </summary>
    [DataField]
    public float DefaultOffset = -0.1f;

    /// <summary>
    /// Client-side vertical offset the "block" sprite layer rises to over the course of Preparing.
    /// </summary>
    [DataField]
    public float PreparingOffset = 0.1f;

    /// <summary>
    /// Client-side vertical offset the "block" sprite layer snaps down to the instant crushing
    /// occurs, then slowly rises back to <see cref="DefaultOffset"/> over Recovering.
    /// </summary>
    [DataField]
    public float CrushOffset = -0.4f;

    /// <summary>
    /// Portion of Preparing (at the end) spent falling from <see cref="PreparingOffset"/> down to
    /// <see cref="CrushOffset"/>, timed so the fall finishes exactly when Preparing ends and
    /// crushing occurs. Must be less than <see cref="PreparingDuration"/>.
    /// </summary>
    [DataField]
    public TimeSpan FallDuration = TimeSpan.FromSeconds(0.3);

    /// <summary>
    /// Portion of Recovering (at the start) spent holding at <see cref="CrushOffset"/> before
    /// rising back to <see cref="DefaultOffset"/>. Must be less than <see cref="RecoveringDuration"/>.
    /// </summary>
    [DataField]
    public TimeSpan HoldDuration = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Entity spawned client-side (purely visual, not networked) at the press's position the
    /// instant it crushes.
    /// </summary>
    [DataField]
    public EntProtoId? CrushVFX;

    /// <summary>
    /// Half-life (in seconds) used to smoothly ease the "block" layer's offset back to
    /// <see cref="DefaultOffset"/> whenever Idle (e.g. power was cut mid-cycle), instead of
    /// snapping there instantly. Lower is snappier.
    /// </summary>
    [DataField]
    public float IdleEaseHalfLife = 0.1f;
}

[Serializable, NetSerializable]
public enum CEPressState : byte
{
    Idle,
    Preparing,
    Crushing,
    Recovering,
}
