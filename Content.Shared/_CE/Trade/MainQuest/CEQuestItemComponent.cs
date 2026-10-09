using Robust.Shared.GameStates;

namespace Content.Shared._CE.Trade.MainQuest;

/// <summary>
/// An ordinal main quest item, e.g. a lightheart shard needed for the Restoration Ritual.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEQuestItemComponent : Component
{
    /// <summary>
    /// 1-based ordinal of this item: the ritual needs every index from 1 to the round's shard count.
    /// </summary>
    [DataField(required: true)]
    public int Index;
}
