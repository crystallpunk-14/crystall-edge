using Content.Shared._DND.Equipment;
using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Features;

/// <summary>
/// The weapon kinds whose mastery properties this character can use.
/// </summary>
[RegisterComponent]
public sealed partial class DndWeaponMasteryComponent : Component
{
    [DataField]
    public List<ProtoId<DndWeaponKindPrototype>> Kinds = new();

    /// <summary>
    /// One change is available after a long rest.
    /// </summary>
    [DataField]
    public int SwapsRemaining;

    /// <summary>
    /// The drill action entity, so a long rest can update the use count shown on it.
    /// </summary>
    [DataField]
    public EntityUid? Drill;

    [DataField]
    public TimeSpan CleaveReadyAt;

    [DataField]
    public TimeSpan NickReadyAt;

    [DataField]
    public EntityUid? VexTarget;

    [DataField]
    public TimeSpan VexUntil;
}
