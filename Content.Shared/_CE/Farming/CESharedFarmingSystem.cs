using Content.Shared._CE.Farming.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Farming;

public abstract partial class CESharedFarmingSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected EntityQuery<CEPlantComponent> PlantQuery;
    protected EntityQuery<CESeedComponent> SeedQuery;
    protected EntityQuery<SolutionContainerManagerComponent> SolutionQuery;

    public override void Initialize()
    {
        base.Initialize();
        InitializeInteractions();

        PlantQuery = GetEntityQuery<CEPlantComponent>();
        SeedQuery = GetEntityQuery<CESeedComponent>();
        SolutionQuery = GetEntityQuery<SolutionContainerManagerComponent>();

        SubscribeLocalEvent<CEPlantComponent, ExaminedEvent>(OnExamine);
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
        Dirty(ent);
    }

    public void AffectResource(Entity<CEPlantComponent> ent, float resourceDelta)
    {
        if (resourceDelta == 0)
            return;

        ent.Comp.Resource = MathHelper.Clamp(ent.Comp.Resource + resourceDelta, 0, ent.Comp.ResourceMax);
        Dirty(ent);
    }

    public void AffectGrowth(Entity<CEPlantComponent> ent, float growthDelta)
    {
        if (growthDelta == 0)
            return;

        ent.Comp.GrowthLevel = MathHelper.Clamp01(ent.Comp.GrowthLevel + growthDelta);
        Dirty(ent);
    }

    [Serializable, NetSerializable]
    public sealed partial class CEPlantSeedDoAfterEvent : DoAfterEvent
    {
        [DataField(required:true)]
        public NetCoordinates Coordinates;

        public CEPlantSeedDoAfterEvent(NetCoordinates coordinates)
        {
            Coordinates = coordinates;
        }

        public override DoAfterEvent Clone() => this;
    }

    [Serializable, NetSerializable]
    public sealed partial class CEPlantGatherDoAfterEvent : SimpleDoAfterEvent;
}
