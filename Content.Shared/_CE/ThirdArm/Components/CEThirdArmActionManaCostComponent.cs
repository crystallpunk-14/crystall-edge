namespace Content.Shared._CE.ThirdArm.Components;

/// <summary>
///     Generic mana cost for any action granted by a third arm module (via CEThirdArmModuleActionsGrantComponent).
///     Placed on the ACTION entity itself. Gated/charged on ActionAttemptEvent, before the action's own event
///     fires - so any module's action just needs this component, no per-action mana-check code.
/// </summary>
[RegisterComponent]
public sealed partial class CEThirdArmActionManaCostComponent : Component
{
    [DataField(required: true)]
    public float ManaCost;
}
