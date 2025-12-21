using Content.Shared._CE.Power.Components;
using Content.Shared.Audio;
using Content.Shared.Destructible;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Radiation.Components;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Power;

public abstract partial class CESharedPowerSystem : EntitySystem
{
    [Dependency] protected readonly SharedPointLightSystem PointLight = default!;
    [Dependency] protected readonly UseDelaySystem UseDelay = default!;
    [Dependency] protected readonly SharedAmbientSoundSystem Ambient = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly PowerCellSystem PowerCell = default!;
    [Dependency] protected readonly SharedBatterySystem Battery = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private readonly EntProtoId _irradiationProto = "CERadiationSourceVFX";

    protected EntityQuery<BatteryComponent> BatteryQuery;

    public override void Initialize()
    {
        base.Initialize();
        InitializeGlove();

        BatteryQuery = GetEntityQuery<BatteryComponent>();

        SubscribeLocalEvent<CEIrradiateOnDestroyComponent, DestructionEventArgs>(OnBatteryDestroyed);
    }

    private void OnBatteryDestroyed(Entity<CEIrradiateOnDestroyComponent> ent, ref DestructionEventArgs args)
    {
        if (!TryComp<BatteryComponent>(ent, out var battery))
            return;

        if (battery.LastCharge <= 0f)
            return;

        Irradiate(Transform(ent).Coordinates, battery.LastCharge, ent.Comp.Time);
    }

    public void Irradiate(EntityCoordinates position, float charge, TimeSpan seconds)
    {
        var vfx = SpawnAtPosition(_irradiationProto, position);

        var totalSec = (float)seconds.TotalSeconds;
        var radiation = EnsureComp<RadiationSourceComponent>(vfx);
        radiation.Enabled = true;
        radiation.Intensity = charge / totalSec;

        var timeDespawn = EnsureComp<TimedDespawnComponent>(vfx);
        timeDespawn.Lifetime = totalSec;
    }
}
