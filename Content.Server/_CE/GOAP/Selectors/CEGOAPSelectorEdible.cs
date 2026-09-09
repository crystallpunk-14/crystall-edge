using System.Numerics;
using Content.Server._CE.GOAP.Navigation;
using Content.Server.Nutrition.Components;
using Content.Shared._CE.Containers;
using Content.Shared._CE.Cooking.Components;
using Content.Shared._CE.Cooking.Prototypes;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityConditions;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GOAP.Selectors;

/// <summary>
/// Selects an ordinary edible, including accessible contents of an open container.
/// Preferences narrow AI selection; they do not change physical digestibility.
/// </summary>
[DataDefinition]
public sealed partial class CEGOAPSelectorEdible
    : CEGOAPTargetSelectorBase<CEGOAPSelectorEdible>, ICEGOAPTargetBackoffSelector
{
    [DataField(required: true)]
    public float Range;

    [DataField]
    public ProtoId<EdiblePrototype> Edible = IngestionSystem.Food;

    [DataField]
    public HashSet<ProtoId<CEFoodTagPrototype>> AllowedFoodTags = new();

    [DataField]
    public HashSet<ProtoId<CEFoodTagPrototype>> ForbiddenFoodTags = new();

    /// <summary>Optional conditions on the edible's solution, e.g. water fraction.</summary>
    [DataField]
    public EntityCondition[]? SolutionConditions;
}

public sealed partial class CEGOAPSelectorEdibleSystem : CEGOAPTargetSelectorSystem<CEGOAPSelectorEdible>
{
    [Dependency] private CEGOAPTargetBackoffSystem _backoff = default!;
    [Dependency] private CEOpenContainerSystem _openContainers = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Resolve(ref CEGOAPSelectorResolveEvent<CEGOAPSelectorEdible> ev)
    {
        var selector = ev.Selector;
        if (!float.IsFinite(selector.Range) || selector.Range < 0f ||
            !TryComp(ev.Agent, out TransformComponent? origin))
            return;

        var agent = ev.Agent;
        var position = _transform.GetWorldPosition(origin);
        var bestDistance = selector.Range * selector.Range;
        EntityUid? best = null;
        _backoff.Prune(agent);

        void Consider(EntityUid candidate)
        {
            if (candidate == agent || _backoff.IsRejected(agent, candidate) ||
                !TryComp(candidate, out TransformComponent? xform) || xform.MapID != origin.MapID)
                return;

            var distance = Vector2.DistanceSquared(position, _transform.GetWorldPosition(xform));
            if (!float.IsFinite(distance) || distance > bestDistance ||
                distance.Equals(bestDistance) && best is { } current && candidate.Id > current.Id ||
                !_interaction.IsAccessible(agent, candidate) ||
                !CanSelect(agent, candidate, selector))
                return;

            best = candidate;
            bestDistance = distance;
        }

        foreach (var edible in _lookup.GetEntitiesInRange<EdibleComponent>(
                     origin.Coordinates, selector.Range, LookupFlags.Uncontained))
            Consider(edible.Owner);

        // Typed edible lookup cannot discover occupants of non-edible holders.
        // Traverse only explicit open containers; never recurse into bags or inventories.
        foreach (var host in _lookup.GetEntitiesInRange<CEOpenContainerComponent>(
                     origin.Coordinates, selector.Range, LookupFlags.Uncontained))
        {
            if (!TryComp(host.Owner, out ContainerManagerComponent? manager))
                continue;

            foreach (var container in _containers.GetAllContainers(host.Owner, manager))
            {
                if (!_openContainers.CanAccessContents(agent, container))
                    continue;

                foreach (var occupant in container.ContainedEntities)
                    Consider(occupant);
            }
        }

        if (best is not { } target)
            return;

        ev.Entity = target;
        ev.Position = Transform(target).Coordinates;
    }

    private bool CanSelect(EntityUid consumer, EntityUid candidate, CEGOAPSelectorEdible selector)
    {
        if (!TryComp(candidate, out EdibleComponent? edible) ||
            _ingestion.GetEdibleType((candidate, edible)) != selector.Edible)
            return false;

        if (selector.Edible == IngestionSystem.Food)
        {
            if (_ingestion.TotalNutrition((candidate, edible)) <= 0f ||
                !HasComp<IgnoreBadFoodComponent>(consumer) && HasComp<BadFoodComponent>(candidate) ||
                !MatchesFoodTags(candidate, selector))
                return false;
        }
        else if (selector.Edible != IngestionSystem.Drink ||
                 _ingestion.TotalHydration((candidate, edible)) <= 0f ||
                 HasComp<BadDrinkComponent>(candidate))
        {
            return false;
        }

        return _ingestion.CanIngest(consumer, candidate) &&
            _ingestion.CanConsume(consumer, candidate) &&
            (selector.SolutionConditions == null ||
             _solutions.TryGetSolution(candidate, edible.Solution, out var solution, out _) &&
             _conditions.TryConditions(solution.Value.Owner, selector.SolutionConditions, consumer));
    }

    private bool MatchesFoodTags(EntityUid food, CEGOAPSelectorEdible selector)
    {
        if (!TryComp(food, out CEFoodTagComponent? tags))
            return selector.AllowedFoodTags.Count == 0;

        var allowed = selector.AllowedFoodTags.Count == 0;
        foreach (var tag in tags.Tags)
        {
            if (selector.ForbiddenFoodTags.Contains(tag))
                return false;

            allowed |= selector.AllowedFoodTags.Contains(tag);
        }

        return allowed;
    }
}
