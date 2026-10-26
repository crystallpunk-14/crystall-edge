namespace Content.Shared._DND.Equipment;

/// <summary>
/// Size used by mastery that pushes. A creature without this is Medium.
/// </summary>
[RegisterComponent]
public sealed partial class DndCreatureSizeComponent : Component
{
    [DataField]
    public DndCreatureSize Size = DndCreatureSize.Medium;
}
