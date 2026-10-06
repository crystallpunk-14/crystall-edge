using Content.Server._CE.Objectives.Components;
using Content.Shared._CE.Currency;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Trade;
using Content.Shared.Mind;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveTradeSpendConditionComponent"/> - increments
/// as the holder's mind spends currency at any trading shop, wherever that money came from.
/// </summary>
public sealed partial class CEObjectiveTradeSpendConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveTradeSpendConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        var target = ent.Comp.TargetGoldSpend * CESharedCurrencySystem.GP.Value;
        if (target <= 0)
            return;

        args.Progress = (float) ent.Comp.AmountSpent / target;
    }

    [SubscribeLocalEvent]
    private void OnTradeCompleted(ref CETradeCompletedEvent args)
    {
        if (args.Paid <= 0)
            return;

        AddSpent(args.Buyer, args.Paid);
    }

    private void AddSpent(EntityUid buyer, int amount)
    {
        if (!_mind.TryGetMind(buyer, out var mindId, out _))
            return;

        if (!TryComp<CEObjectiveHolderComponent>(mindId, out var holder))
            return;

        foreach (var objectiveUid in holder.Objectives)
        {
            if (!TryComp<CEObjectiveTradeSpendConditionComponent>(objectiveUid, out var condition))
                continue;

            condition.AmountSpent += amount;
            _objectives.RefreshObjectiveProgress(objectiveUid);
        }
    }
}
