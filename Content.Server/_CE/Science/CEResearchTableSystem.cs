using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science;

public sealed partial class CEResearchTableSystem : CESharedResearchTableSystem
{
    [Dependency] private CEMagicEssenceSystem _essence = default!;
    [Dependency] private CEScienceSystem _science = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableMergeAspectsMessage>(OnMergeAspects);
    }

    private void OnMergeAspects(Entity<CEResearchTableComponent> ent, ref CEResearchTableMergeAspectsMessage args)
    {
        if (!_essence.TryGetMergeResult(args.First, args.Second, out var result))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(args.Actor);
        var cost = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> { [args.First] = 1, [args.Second] = 1 };

        if (!_science.TrySpendPoints((args.Actor, data), cost))
            return;

        _science.GrantPoints((args.Actor, data), new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> { [result] = 1 });
    }
}
