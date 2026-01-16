using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Thief;

/// <summary>
/// Must be found on unique, irreplaceable entities, the number of which is limited on the map.
/// When stolen by a thief, they give him skill points.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CETheftValueComponent : Component
{
    /// <summary>
    /// Relative theft value of this entity for thieves.
    /// Higher values indicate more valuable or important targets that grant more reward when stolen.
    /// </summary>
    [DataField]
    public float Difficulty = 1f;
}

public sealed partial class CEThiefShowTreasuresEvent : InstantActionEvent;
