namespace Content.Shared._DND.Combat;

/// <summary>
/// This attack's damage already had D&amp;D resistance applied. The following damage modify must not halve it again.
/// </summary>
[RegisterComponent]
public sealed partial class DndAttackDamageResolvedComponent : Component
{
}
