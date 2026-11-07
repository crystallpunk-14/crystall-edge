using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.ActionBlocker;

/// <summary>
/// Status effect that blocks selected kinds of interaction for the entity it is applied to.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEActionBlockerStatusEffectComponent : Component
{
    /// <summary>
    /// Blocks actions that check <c>CheckCanInteract</c>.
    /// </summary>
    [DataField]
    public bool BlockActions;

    /// <summary>
    /// Blocks generic interaction with the world: clicking doors, containers, workbenches, UIs.
    /// </summary>
    [DataField]
    public bool BlockInteract;

    [DataField]
    public bool BlockUse;

    [DataField]
    public bool BlockAttack;

    [DataField]
    public bool BlockMove;

    [DataField]
    public bool BlockThrow;

    [DataField]
    public bool BlockDrop;

    [DataField]
    public bool BlockPickup;

    [DataField]
    public bool BlockPull;

    [DataField]
    public bool BlockStand;

    [DataField]
    public bool BlockEquip;

    [DataField]
    public bool BlockUnequip;
}
