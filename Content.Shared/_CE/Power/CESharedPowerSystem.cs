using Content.Shared.Radiation.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Shared._CE.Power;

public abstract partial class CESharedPowerSystem : EntitySystem
{
    public static EntProtoId IrradiationProto = "CERadiationSourceVFX";

    public void Irradiate(EntityCoordinates position, float charge, TimeSpan seconds)
    {
        var vfx = SpawnAtPosition(IrradiationProto, position);

        var totalSec = (float)seconds.TotalSeconds;
        var radiation = EnsureComp<RadiationSourceComponent>(vfx);
        radiation.Enabled = true;
        radiation.Intensity = charge / totalSec;

        var timeDespawn = EnsureComp<TimedDespawnComponent>(vfx);
        timeDespawn.Lifetime = totalSec;
    }
}
