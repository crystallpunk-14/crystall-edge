using Content.Shared._CE.Satiation;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._CE.Satiation;

public sealed partial class CESatiationSystem : CESharedSatiationSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CESatiationsComponent>();
        while (query.MoveNext(out var uid, out var satiationComp))
        {
            if (satiationComp.NextUpdateTime > _timing.CurTime)
                continue;

            satiationComp.NextUpdateTime = _timing.CurTime + TimeSpan.FromSeconds(1f);

            foreach (var (satiationProto, _) in satiationComp.Satiations)
            {
                if (!_proto.Resolve(satiationProto, out var satiationType))
                    continue;

                // Apply decay rate (negative value to decrease satiation)
                EditSatiationLevel((uid, satiationComp), satiationProto, -satiationType.DecayRate);
            }
        }
    }
}
