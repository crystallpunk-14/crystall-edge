using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Robust.Shared.Player;

namespace Content.Shared._CE.RadialConstruction;

public sealed partial class CERadialConstructionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CERadialConstructionComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<CERadialConstructionComponent, CERadialConstructionMessage>(OnRadialConstructionMessage);
    }

    private void OnInteract(Entity<CERadialConstructionComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<ToolComponent>(args.Used, out var tool))
            return;

        var qualities = tool.Qualities;
        if (!qualities.Contains(ent.Comp.RequiredQuality))
            return;

        args.Handled = true;

        // Open the radial menu UI on the client
        var uiSystem = EntityManager.System<SharedUserInterfaceSystem>();
        uiSystem.OpenUi(ent.Owner, CERadialConstructionUiKey.Key, args.User);
    }

    private void OnRadialConstructionMessage(Entity<CERadialConstructionComponent> ent, ref CERadialConstructionMessage args)
    {
        // Log the selected prototype
        Logger.Info($"Selected craft option: {args.ProtoId}");
    }
}
