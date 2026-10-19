using System.Text;
using Content.Server._CE.GameTicking;
using Content.Server._CE.Objectives.Components;
using Content.Shared._CE.Currency;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.ResourceManager;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles <see cref="CEObjectiveLucsonSphereRestoreConditionComponent"/>: lists the round's prices in
/// the objective's description and reports binary progress - complete only once the Restoration
/// Ritual has succeeded.
/// </summary>
public sealed partial class CEObjectiveLucsonSphereRestoreConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private CEMurkConsumingRuleSystem _murkConsuming = default!;
    [Dependency] private CESharedCurrencySystem _currency = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    [SubscribeLocalEvent]
    private void OnInitialize(Entity<CEObjectiveLucsonSphereRestoreConditionComponent> ent, ref CEInitializeObjectiveEvent args)
    {
        UpdateDescription(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveLucsonSphereRestoreConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        var query = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
        args.Progress = query.MoveNext(out _, out var sphere) && sphere.State == CEMurkSphereState.Success ? 1f : 0f;
    }

    [SubscribeLocalEvent]
    private void OnPricesRolled(CEMainQuestPricesRolledEvent args)
    {
        var query = EntityQueryEnumerator<CEObjectiveLucsonSphereRestoreConditionComponent>();
        while (query.MoveNext(out var uid, out var condition))
        {
            UpdateDescription((uid, condition));
        }
    }

    // The ritual's success ends the round right after the state change, so progress is refreshed
    // here, before the round-end summary reads it.
    [SubscribeLocalEvent]
    private void OnSphereStateChanged(CEMurkSphereStateChangedEvent args)
    {
        var query = EntityQueryEnumerator<CEObjectiveLucsonSphereRestoreConditionComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _objectives.RefreshObjectiveProgress(uid);
        }
    }

    private void UpdateDescription(Entity<CEObjectiveLucsonSphereRestoreConditionComponent> ent)
    {
        var builder = new StringBuilder(Loc.GetString(ent.Comp.Description));

        if (!_murkConsuming.TryGetPriceCount(out var priceCount) || priceCount == 0)
        {
            builder.Append('\n');
            builder.Append(Loc.GetString(ent.Comp.UnknownPrices));
            _metaData.SetEntityDescription(ent, builder.ToString());
            return;
        }

        _murkConsuming.TryGetShardCount(out var shardCount);

        for (var index = 1; index <= priceCount; index++)
        {
            if (!_murkConsuming.TryGetPrice(index, out var price))
                continue;

            var reward = index <= shardCount
                ? Loc.GetString("ce-objective-city-restore-reward-shard", ("index", index))
                : Loc.GetString("ce-objective-city-restore-reward-book");

            builder.Append('\n');
            builder.Append(Loc.GetString("ce-objective-city-restore-price",
                ("reward", reward),
                ("cost", GetCostTitle(price.Cost, price.Pay))));
        }

        _metaData.SetEntityDescription(ent, builder.ToString());
    }

    private string GetCostTitle(List<CEResourceRequirement> cost, int pay)
    {
        var parts = new List<string>();
        foreach (var requirement in cost)
        {
            var title = requirement.GetRequirementTitle(_proto);
            if (!string.IsNullOrEmpty(title))
                parts.Add(title);
        }

        if (pay > 0)
            parts.Add(_currency.GetCurrencyPrettyString(pay));

        return string.Join(", ", parts);
    }
}
