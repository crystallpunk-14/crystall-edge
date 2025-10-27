/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Workbench.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Workbench;

public abstract class CESharedWorkbenchSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed partial class CECraftDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public ProtoId<CEWorkbenchRecipePrototype> Recipe = default!;

    public override DoAfterEvent Clone() => this;
}
