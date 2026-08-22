using System.Text;
using Content.Shared._CE.InfusionAltar.Prototypes;
using Content.Shared._CE.Skill;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.InfusionAltar;

/// <summary>
/// Contributes to <see cref="CEGetSkillEffectEvent"/>: describes which infusion altar recipes
/// a skill unlocks.
/// </summary>
public sealed partial class CEInfusionAltarSkillEffectSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGetSkillEffectEvent>(OnGetSkillEffect);
    }

    private void OnGetSkillEffect(ref CEGetSkillEffectEvent args)
    {
        var items = new List<string>();

        foreach (var recipe in _proto.EnumeratePrototypes<CEInfusionAltarRecipePrototype>())
        {
            if (recipe.RequiredSkill is not { } required || required != args.Skill)
                continue;

            if (!_proto.TryIndex(recipe.Result, out var result))
                continue;

            items.Add(result.Name);
        }

        if (items.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.Append(Loc.GetString("ce-skill-effect-infusion-altar-header"));
        foreach (var item in items)
        {
            sb.Append('\n');
            sb.Append(Loc.GetString("ce-skill-effect-list-item", ("item", item)));
        }

        args.Effects.Add(sb.ToString());
    }
}
