using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._CE.ThirdArm.Components;

[RegisterComponent]
public sealed partial class CEThirdArmComponent : Component
{
    public const string ModuleSlotId = "module_slot";

    [DataField]
    public ItemSlot ModuleSlot = new();

    /// <summary>
    /// Client-only bookkeeping of sprite layer keys currently added by the inserted module's IconLayers.
    /// </summary>
    public HashSet<string> RevealedLayers = new();
}
