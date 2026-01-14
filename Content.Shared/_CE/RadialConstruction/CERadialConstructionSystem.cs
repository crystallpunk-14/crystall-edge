using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._CE.RadialConstruction;

public sealed partial class CERadialConstructionSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CERadialConstructionComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<CERadialConstructionComponent, CERadialConstructionMessage>(OnRadialConstructionMessage);
        SubscribeLocalEvent<CERadialConstructionComponent, CERadialConstructionDoAfterEvent>(OnDoAfterComplete);
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
        // Validate that the selected prototype is in the available list
        if (!ent.Comp.AvailablePrototypes.Contains(args.ProtoId))
            return;


        var doAfter = new CERadialConstructionDoAfterEvent { TargetPrototype = args.ProtoId };
        // Start the DoAfter
        var doAfterArgs = new DoAfterArgs(EntityManager, args.Actor, ent.Comp.Delay, doAfter, ent.Owner, ent.Owner)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfterComplete(Entity<CERadialConstructionComponent> ent, ref CERadialConstructionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        _audio.PlayPvs(ent.Comp.Sound, ent.Owner);

        // Get the position and rotation before deleting the frame
        var xform = Transform(ent.Owner);
        var coordinates = xform.Coordinates;
        var rotation = xform.LocalRotation;

        // Delete the construction frame
        PredictedQueueDel(ent);

        // Spawn the target entity
        var spawned = PredictedSpawnAtPosition(args.TargetPrototype, coordinates);

        // Apply the same rotation
        var spawnedXform = Transform(spawned);
        spawnedXform.LocalRotation = rotation;
    }
}
