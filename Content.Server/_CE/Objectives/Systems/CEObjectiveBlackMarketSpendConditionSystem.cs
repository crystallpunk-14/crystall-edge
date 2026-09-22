using Content.Server._CE.Objectives.Components;
using Content.Shared._CE.Currency;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Trading;
using Content.Shared.Mind;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveBlackMarketSpendConditionComponent"/> - increments
/// as the holder's mind spends currency on the black market, wherever that money came from.
/// </summary>
public sealed partial class CEObjectiveBlackMarketSpendConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveBlackMarketSpendConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        var target = ent.Comp.TargetGoldSteal * CESharedCurrencySystem.GP.Value;
        if (target <= 0)
            return;

        args.Progress = (float) ent.Comp.AmountSpent / target;
    }

    [SubscribeLocalEvent]
    private void OnPlatformPurchase(ref CEPlatformPurchaseEvent args)
    {
        if (args.Faction != "BlackMarket")
            return;

        if (!_mind.TryGetMind(args.Buyer, out var mindId, out _))
            return;

        if (!TryComp<CEObjectiveHolderComponent>(mindId, out var holder))
            return;

        foreach (var objectiveUid in holder.Objectives)
        {
            if (!TryComp<CEObjectiveBlackMarketSpendConditionComponent>(objectiveUid, out var condition))
                continue;

            condition.AmountSpent += args.Price;
            _objectives.RefreshObjectiveProgress(objectiveUid);
        }
    }
}
