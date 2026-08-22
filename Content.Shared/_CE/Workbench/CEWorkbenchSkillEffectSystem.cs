using System.Text;
using Content.Shared._CE.Skill;
using Content.Shared._CE.Workbench.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Workbench;

/// <summary>
/// Contributes to <see cref="CEGetSkillEffectEvent"/>: describes which workbench recipes a
/// skill unlocks, grouped by the workbench they're crafted at.
/// </summary>
public sealed partial class CEWorkbenchSkillEffectSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGetSkillEffectEvent>(OnGetSkillEffect);
    }

    private void OnGetSkillEffect(ref CEGetSkillEffectEvent args)
    {
        var byStation = new Dictionary<string, List<string>>();

        foreach (var recipe in _proto.EnumeratePrototypes<CEWorkbenchRecipePrototype>())
        {
            if (recipe.Abstract)
                continue;

            if (recipe.RequiredSkill is not { } required || required != args.Skill)
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
                ? Loc.GetString("ce-skill-effect-workbench-header", ("station", station))
                : Loc.GetString("ce-skill-effect-workbench-header-generic");

            var sb = new StringBuilder();
            sb.Append(header);
            foreach (var item in items)
            {
                sb.Append('\n');
                sb.Append(Loc.GetString("ce-skill-effect-list-item", ("item", item)));
            }

            args.Effects.Add(sb.ToString());
        }
    }
}
