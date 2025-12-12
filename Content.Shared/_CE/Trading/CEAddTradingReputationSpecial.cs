using Content.Shared._CE.Trading.Prototypes;
using Content.Shared._CE.Trading.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading;

public sealed partial class CEAddTradingReputationSpecial : JobSpecial
{
    [DataField]
    public HashSet<ProtoId<CETradingFactionPrototype>> Factions = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var tradeSys = entMan.System<CESharedTradingPlatformSystem>();

        foreach (var faction in Factions)
        {
            tradeSys.AddContractToPlayer(mob, faction);
        }
    }
}
