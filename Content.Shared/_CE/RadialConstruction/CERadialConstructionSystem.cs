using Content.Shared.Interaction;
using Content.Shared.Tools.Components;

namespace Content.Shared._CE.RadialConstruction;

public sealed partial class CERadialConstructionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CERadialConstructionComponent, InteractUsingEvent>(OnInteract);
    }

    private void OnInteract(Entity<CERadialConstructionComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<ToolComponent>(args.Used, out var tool))
            return;

        var qualities = tool.Qualities;
        if (!qualities.Contains(ent.Comp.RequiredQuality))
            return;

        args.Handled = true;
    }
}
