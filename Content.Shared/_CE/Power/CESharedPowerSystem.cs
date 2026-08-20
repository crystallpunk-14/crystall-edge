using Content.Shared._CE.Power.Components;
using Content.Shared.Audio;
using Content.Shared.Destructible;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Power;

public abstract partial class CESharedPowerSystem : EntitySystem
{
    [Dependency] protected SharedPointLightSystem PointLight = default!;
    [Dependency] protected UseDelaySystem UseDelay = default!;
    [Dependency] protected SharedAmbientSoundSystem Ambient = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected PowerCellSystem PowerCell = default!;
    [Dependency] protected SharedBatterySystem Battery = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedRadiationSystem _radiation = default!;

    private readonly EntProtoId _irradiationProto = "CERadiationSourceVFX";

    protected EntityQuery<BatteryComponent> BatteryQuery;

    public override void Initialize()
    {
        base.Initialize();
        InitializeGlove();

        BatteryQuery = GetEntityQuery<BatteryComponent>();

        SubscribeLocalEvent<CEIrradiateOnDestroyComponent, DestructionEventArgs>(OnBatteryDestroyed);
        SubscribeLocalEvent<CEPowerAmbientSoundComponent, PowerChangedEvent>(OnAmbientPowerChanged);
        SubscribeLocalEvent<CEPowerPointLightComponent, PowerChangedEvent>(OnLightPowerChanged);
    }

    private void OnLightPowerChanged(Entity<CEPowerPointLightComponent> ent, ref PowerChangedEvent args)
    {
        PointLight.SetEnabled(ent, args.Powered);
    }

    private void OnAmbientPowerChanged(Entity<CEPowerAmbientSoundComponent> ent, ref PowerChangedEvent args)
    {
        Ambient.SetAmbience(ent, args.Powered);
    }

    private void OnBatteryDestroyed(Entity<CEIrradiateOnDestroyComponent> ent, ref DestructionEventArgs args)
    {
        if (!TryComp<BatteryComponent>(ent, out var battery))
            return;

        if (battery.LastCharge <= 0f)
            return;

        Irradiate(Transform(ent).Coordinates, battery.LastCharge * ent.Comp.IrradiateCoefficient, ent.Comp.Time);
    }

    public void Irradiate(EntityCoordinates position, float charge, TimeSpan seconds)
    {
        var vfx = SpawnAtPosition(_irradiationProto, position);

        var totalSec = (float)seconds.TotalSeconds;
        EnsureComp<RadiationSourceComponent>(vfx);
        _radiation.SetIntensity(vfx, charge / totalSec);

        var timeDespawn = EnsureComp<TimedDespawnComponent>(vfx);
        timeDespawn.Lifetime = totalSec;
    }
}
