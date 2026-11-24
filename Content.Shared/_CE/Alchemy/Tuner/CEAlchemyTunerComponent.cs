using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Alchemy.Tuner;

/// <summary>
/// A powered device that tunes the alchemy mixtures inside its solution: while it works, every cycle shifts
/// <c>CEAlchemyData.Tune</c> by <see cref="Step"/> towards the limit of the current <see cref="Mode"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEAlchemyTunerComponent : Component
{
    [DataField(required: true)]
    public string SolutionName = string.Empty;

    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    [DataField, AutoNetworkedField]
    public CEAlchemyTunerMode Mode = CEAlchemyTunerMode.Concentrate;

    /// <summary>
    /// Tune shift per cycle.
    /// </summary>
    [DataField]
    public FixedPoint2 Step = FixedPoint2.New(0.05f);

    [DataField]
    public TimeSpan CycleTime = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg", AudioParams.Default.WithVolume(-2f));

    [ViewVariables]
    public DoAfterId? DoAfterId;
}

public enum CEAlchemyTunerMode : byte
{
    /// <summary>
    /// Towards +1: stronger and shorter effects.
    /// </summary>
    Concentrate,

    /// <summary>
    /// Towards -1: weaker and longer effects.
    /// </summary>
    Extend,
}

[Serializable, NetSerializable]
public enum CEAlchemyTunerVisuals : byte
{
    Working,
}

[Serializable, NetSerializable]
public sealed partial class CEAlchemyTunerDoAfter : SimpleDoAfterEvent;
