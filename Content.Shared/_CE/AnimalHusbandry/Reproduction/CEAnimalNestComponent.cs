using Robust.Shared.GameStates;
using Robust.Shared.Localization;

namespace Content.Shared._CE.AnimalHusbandry.Reproduction;

/// <summary>A nest's ItemSlots hold eggs; resident birds reserve separate capacity.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEAnimalNestComponent : Component
{
    [DataField(required: true)] public LocId ExamineMessage;
    [DataField(required: true)] public int ResidentCapacity;
    [DataField(serverOnly: true)] public HashSet<EntityUid> Residents = new();
}

/// <summary>Replicated targeting capability, updated when eggs or resident birds change.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEAnimalNestAvailableComponent : Component;
