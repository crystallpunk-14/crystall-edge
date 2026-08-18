using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.RadialConstruction;

public sealed partial class CERadialConstructionSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CERadialConstructionComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<CERadialConstructionComponent, CERadialConstructionMessage>(OnRadialConstructionMessage);
        SubscribeLocalEvent<CERadialConstructionComponent, CERadialConstructionFinishedEvent>(OnFinished);
        SubscribeLocalEvent<CERadialConstructionComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInteract(Entity<CERadialConstructionComponent> ent, ref InteractUsingEvent args)
    {
        if (GetVariant(ent, args.Used) is not { } variant)
            return;

        args.Handled = true;

        _ui.SetUiState(ent.Owner, CERadialConstructionUiKey.Key, new CERadialConstructionBuiState(variant.AvailablePrototypes));
        _ui.OpenUi(ent.Owner, CERadialConstructionUiKey.Key, args.User);
    }

    private void OnRadialConstructionMessage(Entity<CERadialConstructionComponent> ent, ref CERadialConstructionMessage args)
    {
        // Find a held item whose trigger matches and whose variant actually offers the chosen prototype.
        EntityUid? itemUid = null;
        CERadialConstructionVariant? variant = null;

        foreach (var heldItem in _hands.EnumerateHeld(args.Actor))
        {
            if (GetVariant(ent, heldItem) is not { } candidate || !candidate.AvailablePrototypes.Contains(args.ProtoId))
                continue;

            itemUid = heldItem;
            variant = candidate;
            break;
        }

        if (itemUid == null || variant == null)
            return;

        variant.Trigger.StartUse(EntityManager, itemUid.Value, args.Actor, ent.Owner, ent.Comp.Delay, new CERadialConstructionFinishedEvent(args.ProtoId));
    }

    private void OnFinished(Entity<CERadialConstructionComponent> ent, ref CERadialConstructionFinishedEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Used is not { } used || GetVariant(ent, used) is not { } variant || !variant.AvailablePrototypes.Contains(args.TargetPrototype))
            return;

        args.Handled = true;

        variant.Trigger.Commit(EntityManager, _proto, used);

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

    private void OnExamined(Entity<CERadialConstructionComponent> ent, ref ExaminedEvent args)
    {
        foreach (var variant in ent.Comp.Variants)
        {
            if (variant.Trigger.GetExamineHint(_proto) is not { } hint)
                continue;

            args.PushMarkup(Loc.GetString("ce-radial-construction-examine", ("toolName", hint)));
        }
    }

    private CERadialConstructionVariant? GetVariant(Entity<CERadialConstructionComponent> ent, EntityUid item)
    {
        foreach (var variant in ent.Comp.Variants)
        {
            if (variant.Trigger.Matches(EntityManager, _proto, item))
                return variant;
        }

        return null;
    }
}
