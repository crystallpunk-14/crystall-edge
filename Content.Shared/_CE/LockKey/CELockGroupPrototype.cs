using Robust.Shared.Prototypes;

namespace Content.Shared._CE.LockKey;

/// <summary>
/// A set of lock types stored together, e.g. all tavern rooms.
/// Key racks are filled with keys for every lock type of their groups found on the station.
/// </summary>
[Prototype("lockGroup")]
public sealed partial class CELockGroupPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
}
