using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.RadialConstruction;

public sealed partial class CERadialConstructionSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

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
        if (!TryComp<ToolComponent>(args.Used, out var tool))
            return;

        var qualities = tool.Qualities;
        if (!qualities.Contains(ent.Comp.RequiredQuality))
            return;

        args.Handled = true;

        // Open the radial menu UI on the client
        var uiSystem = EntityManager.System<SharedUserInterfaceSystem>();
        var toolNetEntity = EntityManager.GetNetEntity(args.Used);
        uiSystem.OpenUi(ent.Owner, CERadialConstructionUiKey.Key, args.User);
        uiSystem.SetUiState(ent.Owner, CERadialConstructionUiKey.Key, new CERadialConstructionUIState(toolNetEntity));
    }

    private void OnRadialConstructionMessage(Entity<CERadialConstructionComponent> ent, ref CERadialConstructionMessage args)
    {
        // Validate that the selected prototype is in the available list
        if (!ent.Comp.AvailablePrototypes.Contains(args.ProtoId))
            return;

        var toolUid = GetEntity(args.ToolUid);

        _tool.UseTool(
            toolUid,
            args.Actor,
            ent.Owner,
            ent.Comp.Delay,
            ent.Comp.RequiredQuality,
            new CERadialConstructionFinishedEvent(args.ProtoId)
        );
    }

    private void OnFinished(Entity<CERadialConstructionComponent> ent, ref CERadialConstructionFinishedEvent args)
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

    private void OnExamined(Entity<CERadialConstructionComponent> ent, ref ExaminedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.RequiredQuality, out var qualityProto))
            return;

        args.PushMarkup(Loc.GetString("ce-radial-construction-examine", ("toolName", Loc.GetString(qualityProto.ToolName))));
    }
}
