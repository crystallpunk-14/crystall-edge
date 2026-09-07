using Content.Server._CE.GOAP;
using Content.Server._CE.GOAP.Classifiers;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared.EntityConditions;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.AnimalHusbandry.Reproduction;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEAnimalMateComponent : Component
{
    [DataField(required: true)] public EntityWhitelist MateWhitelist = default!;
    [DataField] public EntityCondition[] Conditions = [];
    [DataField] public EntityCondition[] MateConditions = [];
    [DataField] public float SearchRange = 12;
    [DataField] public float DayFraction = 0.5f;
    [DataField] public TimeSpan FallbackDayDuration = TimeSpan.FromMinutes(24);
    [DataField] public int FertilizedProducts = 2;
    [DataField] public TimeSpan InteractionDuration = TimeSpan.FromSeconds(1);
    [DataField] public EntProtoId? Effect;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextMating;
    [DataField] public EntityUid? Target;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan InteractionEnd;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan AttemptEnd;
}

public sealed partial class CEGOAPMateAction : CEGOAPActionBase<CEGOAPMateAction>
{
}

public sealed partial class CEAnimalMatingSystem : CEGOAPActionSystem<CEGOAPMateAction>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private NPCSteeringSystem _steering = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    private bool Eligible(EntityUid agent, CEAnimalMateComponent mate, EntityUid target)
    {
        return target != agent && Exists(target) &&
               TryComp<CEAnimalFertilityComponent>(target, out var fertility) && fertility.ProductsRemaining == 0 &&
               _whitelist.IsValid(mate.MateWhitelist, target) &&
               _conditions.TryConditions(target, mate.MateConditions, agent) &&
               (!TryComp<CEGOAPKnowledgeCacheComponent>(target, out var knowledge) || knowledge.Enemies.Count == 0) &&
               Transform(agent).Coordinates.TryDistance(EntityManager, Transform(target).Coordinates, out var distance) &&
               distance <= mate.SearchRange;
    }

    private EntityUid? FindMate(EntityUid agent, CEAnimalMateComponent mate)
    {
        EntityUid? nearest = null;
        var distance = float.MaxValue;
        var coordinates = Transform(agent).Coordinates;
        foreach (var target in _lookup.GetEntitiesInRange(coordinates, mate.SearchRange))
        {
            if (!Eligible(agent, mate, target) ||
                !coordinates.TryDistance(EntityManager, Transform(target).Coordinates, out var candidateDistance) ||
                candidateDistance >= distance)
                continue;
            nearest = target;
            distance = candidateDistance;
        }
        return nearest;
    }

    protected override void OnCanExecute(Entity<CEGOAPComponent> ent, ref CEGOAPActionCanExecuteEvent<CEGOAPMateAction> args)
    {
        args.CanExecute = TryComp<CEAnimalMateComponent>(ent, out var mate) &&
                          _timing.CurTime >= mate.NextMating &&
                          _conditions.TryConditions(ent.Owner, mate.Conditions) &&
                          (mate.Target is { } target && Eligible(ent, mate, target) || FindMate(ent, mate) != null);
    }

    protected override void OnActionStartup(Entity<CEGOAPComponent> ent, ref CEGOAPActionStartupEvent<CEGOAPMateAction> args)
    {
        if (!TryComp<CEAnimalMateComponent>(ent, out var mate))
            return;
        mate.Target = FindMate(ent, mate);
        mate.InteractionEnd = TimeSpan.Zero;
        mate.AttemptEnd = _timing.CurTime + TimeSpan.FromSeconds(20);
    }

    protected override void OnActionUpdate(Entity<CEGOAPComponent> ent, ref CEGOAPActionUpdateEvent<CEGOAPMateAction> args)
    {
        if (!TryComp<CEAnimalMateComponent>(ent, out var mate) || mate.Target is not { } target ||
            !Eligible(ent, mate, target) || !_conditions.TryConditions(ent.Owner, mate.Conditions))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        if (_timing.CurTime >= mate.AttemptEnd)
        {
            mate.NextMating = _timing.CurTime + TimeSpan.FromSeconds(5);
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        var coordinates = Transform(target).Coordinates;
        Transform(ent).Coordinates.TryDistance(EntityManager, coordinates, out var distance);
        // Close coordinates alone do not permit contact across a wall or a closed container.
        if (distance > 0.7f || !_interaction.InRangeAndAccessible(ent.Owner, target))
        {
            mate.InteractionEnd = TimeSpan.Zero;
            if (!TryComp<NPCSteeringComponent>(ent, out var steering) ||
                !steering.Coordinates.TryDistance(EntityManager, coordinates, out var delta) || delta > 0.3f)
            {
                _steering.Unregister(ent);
                _steering.Register(ent, coordinates).Range = 0.6f;
            }
            return;
        }

        _steering.Unregister(ent);
        if (mate.InteractionEnd == TimeSpan.Zero)
            mate.InteractionEnd = _timing.CurTime + mate.InteractionDuration;
        if (_timing.CurTime < mate.InteractionEnd)
            return;

        // Revalidate eligibility at completion so competing mates cannot both fertilize the same clutch.
        var fertility = Comp<CEAnimalFertilityComponent>(target);
        fertility.ProductsRemaining = mate.FertilizedProducts;
        if (mate.Effect is { } effect)
            Spawn(effect, coordinates);
        var day = Transform(ent).MapUid is { } map && TryComp<LightCycleComponent>(map, out var cycle)
            ? cycle.Duration : mate.FallbackDayDuration;
        mate.NextMating = _timing.CurTime + day * mate.DayFraction;
        args.Status = CEGOAPActionStatus.Finished;
    }

    protected override void OnActionShutdown(Entity<CEGOAPComponent> ent, ref CEGOAPActionShutdownEvent<CEGOAPMateAction> args)
    {
        _steering.Unregister(ent);
        if (TryComp<CEAnimalMateComponent>(ent, out var mate))
        {
            mate.Target = null;
            mate.InteractionEnd = TimeSpan.Zero;
        }
    }
}
