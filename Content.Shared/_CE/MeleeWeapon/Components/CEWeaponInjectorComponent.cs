using Content.Shared.FixedPoint;

namespace Content.Shared._CE.MeleeWeapon.Components;

/// <summary>
/// Attacks with this CE weapon inject its solution into the bloodstream of every target hit.
/// </summary>
[RegisterComponent]
public sealed partial class CEWeaponInjectorComponent : Component
{
    /// <summary>
    /// Solution to inject from.
    /// </summary>
    [DataField(required: true)]
    public string Solution = string.Empty;

    /// <summary>
    /// How much is injected into each target. Shared equally if there is not enough for all of them.
    /// </summary>
    [DataField]
    public FixedPoint2 TransferAmount = 1;
}
