using Robust.Shared.Prototypes;

namespace Content.Shared._CE.LockKey;

/// <summary>
/// A lock or key shape, pre-generated at the start of the round.
/// Allows a group of doors and keys to have the same shape within the same round and fit together,
/// but is randomized from round to round
/// </summary>
[Prototype("lockType")]
public sealed partial class CELockTypePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The number of elements that will be generated for the category.
    /// </summary>
    [DataField]
    public int Complexity = 3;

    /// <summary>
    /// Group this lock type belongs to. Key racks of this group get a key for it.
    /// </summary>
    [DataField]
    public ProtoId<CELockGroupPrototype>? Group;

    [DataField]
    public LocId? Name;
}
