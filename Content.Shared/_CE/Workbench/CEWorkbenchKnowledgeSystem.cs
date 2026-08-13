using System.Text;
using Content.Shared._CE.Knowledge;
using Content.Shared._CE.Workbench.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Workbench;

/// <summary>
/// Contributes to <see cref="CEGetKnowledgeEffectEvent"/>: describes which workbench recipes a
/// piece of knowledge unlocks, grouped by the workbench they're crafted at.
/// </summary>
public sealed partial class CEWorkbenchKnowledgeSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGetKnowledgeEffectEvent>(OnGetKnowledgeEffect);
    }

    private void OnGetKnowledgeEffect(ref CEGetKnowledgeEffectEvent args)
    {
        var byStation = new Dictionary<string, List<string>>();

        foreach (var recipe in _proto.EnumeratePrototypes<CEWorkbenchRecipePrototype>())
        {
            if (recipe.Abstract)
                continue;

            if (recipe.RequiredKnowledge is not { } required || required != args.Knowledge)
                continue;

            if (!_proto.TryIndex(recipe.Result, out var result))
                continue;

            var station = recipe.Workbench is { } workbenchId && _proto.TryIndex(workbenchId, out var workbench)
                ? workbench.Name
                : string.Empty;

            if (!byStation.TryGetValue(station, out var items))
                byStation[station] = items = new List<string>();

            items.Add(result.Name);
        }

        foreach (var (station, items) in byStation)
        {
            var header = station.Length > 0
                ? Loc.GetString("ce-knowledge-effect-workbench-header", ("station", station))
                : Loc.GetString("ce-knowledge-effect-workbench-header-generic");

            var sb = new StringBuilder();
            sb.Append(header);
            foreach (var item in items)
            {
                sb.Append('\n');
                sb.Append(Loc.GetString("ce-knowledge-effect-list-item", ("item", item)));
            }

            args.Effects.Add(sb.ToString());
        }
    }
}
