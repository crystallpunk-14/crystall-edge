using Content.Shared._CE.Workbench.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Workbench;

[Serializable, NetSerializable]
public sealed partial class CECraftDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public ProtoId<CEWorkbenchRecipePrototype> Recipe = default!;

    public override DoAfterEvent Clone() => this;
}
