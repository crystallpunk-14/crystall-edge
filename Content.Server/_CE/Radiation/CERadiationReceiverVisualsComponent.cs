using Content.Server._CE.GameTicking;

namespace Content.Server._CE.Radiation;

/// <summary>
/// Stores data for <see cref="CEThiefRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(CERadiationReceiverVisualsSystem))]
public sealed partial class CERadiationReceiverVisualsComponent : Component
{
    [DataField]
    public bool Active;
}
