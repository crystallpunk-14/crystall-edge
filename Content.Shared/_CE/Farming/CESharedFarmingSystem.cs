using Content.Shared._CE.Farming.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Farming;

public abstract partial class CESharedFarmingSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    protected EntityQuery<CEPlantComponent> PlantQuery;
    protected EntityQuery<CEPlantProducingComponent> PlantProducingQuery;
    protected EntityQuery<CESeedComponent> SeedQuery;
    protected EntityQuery<SolutionContainerManagerComponent> SolutionQuery;

    public override void Initialize()
    {
        base.Initialize();
        InitializeSeeds();
        InitializeGather();
        InitializeGatherAdditional();

        PlantQuery = GetEntityQuery<CEPlantComponent>();
        PlantProducingQuery = GetEntityQuery<CEPlantProducingComponent>();
        SeedQuery = GetEntityQuery<CESeedComponent>();
        SolutionQuery = GetEntityQuery<SolutionContainerManagerComponent>();

        SubscribeLocalEvent<CEPlantComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<CEPlantComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }
    private void OnAnchorStateChanged(Entity<CEPlantComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            _destructible.DestroyEntity(ent.Owner);
    }

    private void OnExamine(EntityUid uid, CEPlantComponent component, ExaminedEvent args)
    {
        if (component.Energy <= 0)
            args.PushMarkup(Loc.GetString("ce-farming-low-energy"));

        if (component.Resource <= 0)
            args.PushMarkup(Loc.GetString("ce-farming-low-resources"));
    }

    public void AffectEnergy(Entity<CEPlantComponent> ent, float energyDelta)
    {
        if (energyDelta == 0)
            return;

        ent.Comp.Energy = MathHelper.Clamp(ent.Comp.Energy + energyDelta, 0, ent.Comp.EnergyMax);
        DirtyField(ent, ent.Comp, nameof(CEPlantComponent.Energy));
    }

    public void AffectResource(Entity<CEPlantComponent> ent, float resourceDelta)
    {
        if (resourceDelta == 0)
            return;

        ent.Comp.Resource = MathHelper.Clamp(ent.Comp.Resource + resourceDelta, 0, ent.Comp.ResourceMax);
        DirtyField(ent, ent.Comp, nameof(CEPlantComponent.Resource));
    }

    public void AffectGrowth(Entity<CEPlantComponent> ent, float growthDelta)
    {
        if (growthDelta == 0)
            return;

        ent.Comp.GrowthLevel = MathHelper.Clamp01(ent.Comp.GrowthLevel + growthDelta);
        DirtyField(ent, ent.Comp, nameof(CEPlantComponent.GrowthLevel));
    }
}
