using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Workbench.Press;

/// <summary>
/// Animates the press "block" sprite layer in sync with the autocrafter on the same entity:
/// it rises while a craft is in progress, slams down when the craft completes, then recovers.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CEPressVisualsComponent : Component
{
    /// <summary>
    /// When the current craft started. Zero while not crafting.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan CraftStartTime;

    /// <summary>
    /// When the current craft is expected to complete. Zero while not crafting.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan CraftEndTime;

    /// <summary>
    /// When the last craft completed and the block hit the platform.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan LastCrushTime;

    /// <summary>
    /// How long the block takes to rise back after crushing, including <see cref="HoldDuration"/>.
    /// </summary>
    [DataField]
    public TimeSpan RecoveringDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Portion of the end of a craft spent falling from <see cref="PreparingOffset"/> to <see cref="CrushOffset"/>.
    /// </summary>
    [DataField]
    public TimeSpan FallDuration = TimeSpan.FromSeconds(0.3);

    /// <summary>
    /// Portion of the start of recovering spent holding at <see cref="CrushOffset"/>.
    /// </summary>
    [DataField]
    public TimeSpan HoldDuration = TimeSpan.FromSeconds(0.3);

    [DataField]
    public string BlockLayerKey = "block";

    /// <summary>
    /// Vertical offset of the block while idle.
    /// </summary>
    [DataField]
    public float DefaultOffset = -0.1f;

    /// <summary>
    /// Vertical offset the block rises to while crafting.
    /// </summary>
    [DataField]
    public float PreparingOffset = 0.1f;

    /// <summary>
    /// Vertical offset the block slams down to when the craft completes.
    /// </summary>
    [DataField]
    public float CrushOffset = -0.4f;

    /// <summary>
    /// Half-life (in seconds) used to ease the block back to <see cref="DefaultOffset"/> when a craft
    /// is interrupted. Lower is snappier.
    /// </summary>
    [DataField]
    public float IdleEaseHalfLife = 0.1f;
}
