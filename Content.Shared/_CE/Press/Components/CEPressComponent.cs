using Content.Shared._CE.Press.Systems;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Press.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(CEPressSystem))]
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
    public TimeSpan RecoveringDuration = TimeSpan.FromSeconds(2);

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
}

[Serializable, NetSerializable]
public enum CEPressState : byte
{
    Idle,
    Preparing,
    Crushing,
    Recovering,
}
