using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.Consumption;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Whitelist;
using ChemicalSolution = Content.Shared.Chemistry.Components.Solution;

namespace Content.Server._CE.Consumption;

/// <summary>
/// Runtime context that exposes one selected drainable solution to canonical
/// ingestion. It contains no transfer or needs state of its own.
/// </summary>
[RegisterComponent]
public sealed partial class CESelectedDrainableIngestionComponent : Component
{
    public CEDrainableSolutionConsumableSource SourceDefinition = default!;
    public EntityUid Source;
    public EntityUid Solution;
    public ChemicalSolution SelectedSolution = default!;
    public FixedPoint2 TransferAmount;
}

/// <summary>
/// Adapts non-edible drainable solutions to IngestionSystem's public event seam.
/// IngestionSystem remains responsible for timing, splitting, reactions,
/// stomach transfer and resulting hunger or thirst changes.
/// </summary>
public sealed partial class CEDrainableSolutionConsumableSourceSystem
    : CENearestConsumableSourceSystem<CEDrainableSolutionConsumableSource, DrainableSolutionComponent>
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private OpenableSystem _openable = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DrainableSolutionComponent, EdibleEvent>(
            OnEdible,
            after: [typeof(IngestionSystem)]);
        SubscribeLocalEvent<CESelectedDrainableIngestionComponent, BeforeIngestedEvent>(
            OnBeforeIngested,
            after: [typeof(IngestionSystem)]);
    }

    protected override bool IsProviderValid(
        EntityUid consumer,
        Entity<DrainableSolutionComponent> provider,
        CEDrainableSolutionConsumableSource source)
    {
        return TryGetSource(consumer, provider, source, out _, out _);
    }

    protected override bool CanHandleProvider(
        EntityUid provider,
        CEDrainableSolutionConsumableSource source)
    {
        return base.CanHandleProvider(provider, source) &&
            !HasComp<EdibleComponent>(provider) &&
            _whitelist.CheckBoth(provider, source.ProviderBlacklist, source.ProviderWhitelist);
    }

    protected override bool TryResolveConsumable(
        EntityUid consumer,
        EntityUid provider,
        CEDrainableSolutionConsumableSource source,
        out EntityUid consumable)
    {
        consumable = default;
        if (!TryComp(provider, out DrainableSolutionComponent? drainable) ||
            !TryGetSource(consumer, (provider, drainable), source, out var solutionEntity, out var solution))
            return false;

        if (HasComp<CESelectedDrainableIngestionComponent>(consumer))
            return false;

        var context = AddComp<CESelectedDrainableIngestionComponent>(consumer);
        context.SourceDefinition = source;
        context.Source = provider;
        context.Solution = solutionEntity.Value.Owner;
        context.SelectedSolution = solution;
        context.TransferAmount = source.TransferAmount;

        consumable = provider;
        return true;
    }

    protected override void ReleaseConsumable(
        EntityUid consumer,
        EntityUid provider,
        EntityUid consumable,
        bool consumed,
        CEDrainableSolutionConsumableSource source)
    {
        if (provider != consumable ||
            !TryComp<CESelectedDrainableIngestionComponent>(consumer, out var context) ||
            !ReferenceEquals(context.SourceDefinition, source) ||
            context.Source != provider)
            return;

        RemComp<CESelectedDrainableIngestionComponent>(consumer);
    }

    private bool TryGetSource(
        EntityUid consumer,
        Entity<DrainableSolutionComponent> provider,
        CEDrainableSolutionConsumableSource source,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEntity,
        [NotNullWhen(true)] out ChemicalSolution? solution)
    {
        solutionEntity = null;
        solution = null;

        if (source.Conditions == null || source.TransferAmount <= FixedPoint2.Zero ||
            HasComp<EdibleComponent>(provider.Owner) ||
            !_whitelist.CheckBoth(provider.Owner, source.ProviderBlacklist, source.ProviderWhitelist) ||
            TryComp<OpenableComponent>(provider.Owner, out var openable) &&
            _openable.IsClosed(provider.Owner, consumer, openable, predicted: true) ||
            !_solutions.TryGetDrainableSolution(provider.Owner, out solutionEntity, out solution) ||
            solution.Volume <= FixedPoint2.Zero ||
            !_conditions.TryConditions(solutionEntity.Value.Owner, source.Conditions, consumer) ||
            !_ingestion.CanIngest(consumer, provider.Owner))
            return false;

        return true;
    }

    private void OnEdible(Entity<DrainableSolutionComponent> ent, ref EdibleEvent args)
    {
        if (args.Cancelled ||
            !TryComp<CESelectedDrainableIngestionComponent>(args.User, out var context) ||
            context.Source != ent.Owner)
            return;

        if (!TryGetSource(args.User, ent, context.SourceDefinition, out var solutionEntity, out var solution) ||
            solutionEntity.Value.Owner != context.Solution ||
            args.Solution is { } offered && !ReferenceEquals(offered.Comp.Solution, solution))
        {
            args.Cancelled = true;
            return;
        }

        context.SelectedSolution = solution;
        args.Solution = solutionEntity;
        args.Time += ent.Comp.DrainTime;
    }

    private void OnBeforeIngested(
        Entity<CESelectedDrainableIngestionComponent> ent,
        ref BeforeIngestedEvent args)
    {
        if (args.Cancelled || args.Solution is not { } offered ||
            !ReferenceEquals(offered, ent.Comp.SelectedSolution))
            return;

        if (!TryComp(ent.Comp.Solution, out SolutionComponent? selected) ||
            !ReferenceEquals(offered, selected.Solution) ||
            !TryComp(ent.Comp.Source, out DrainableSolutionComponent? drainable) ||
            !TryGetSource(
                ent.Owner,
                (ent.Comp.Source, drainable),
                ent.Comp.SourceDefinition,
                out var validatedEntity,
                out var validated) ||
            validatedEntity.Value.Owner != ent.Comp.Solution ||
            !ReferenceEquals(offered, validated))
        {
            args.Cancelled = true;
            return;
        }

        args.Transfer = ent.Comp.TransferAmount;
    }
}
