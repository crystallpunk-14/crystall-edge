using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Salary;

/// <summary>
/// Salary that accrues on this action every dawn and is paid out to the performer when the action is used.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CESalaryComponent : Component
{
    /// <summary>
    /// Amount of currency added to <see cref="Unpaid"/> at every dawn.
    /// </summary>
    [DataField]
    public int Salary = 100;

    /// <summary>
    /// Accumulated salary that hasn't been withdrawn yet.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Unpaid;

    [DataField]
    public SoundSpecifier PaySound = new SoundPathSpecifier("/Audio/_CE/Effects/cash.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.1f),
    };
}

public sealed partial class CESalaryActionEvent : InstantActionEvent;
