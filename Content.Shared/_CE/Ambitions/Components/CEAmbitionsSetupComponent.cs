using Robust.Shared.GameStates;

namespace Content.Shared._CE.Ambitions.Components;

/// <summary>
/// Creates ambitious goals for the character and allows them to be rerolled a certain number of times.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(CESharedAmbitionsSystem))]
public sealed partial class CEAmbitionsSetupComponent : Component
{
    [DataField, AutoNetworkedField]
    public int RerollAmount = 10;

    /// <summary>
    /// How much ambition is generated when creating a character?
    /// </summary>
    [DataField]
    public int MaxAmbitions = 3;
}
