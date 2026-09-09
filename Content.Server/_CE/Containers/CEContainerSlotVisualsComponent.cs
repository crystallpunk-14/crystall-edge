using System.Numerics;

namespace Content.Server._CE.Containers;

/// <summary>Authored presentation keyed by existing container IDs. ItemSlots owns the containers.</summary>
[RegisterComponent]
public sealed partial class CEContainerSlotVisualsComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, CEContainerSlotVisual> Slots = new();
}

[DataDefinition]
public sealed partial class CEContainerSlotVisual
{
    [DataField(required: true)]
    public Vector2 Offset;

    [DataField]
    public Angle Rotation;
}
