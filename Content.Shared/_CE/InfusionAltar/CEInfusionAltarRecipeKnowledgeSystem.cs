using System.Text;
using Content.Shared._CE.Knowledge;
using Content.Shared._CE.InfusionAltar.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.InfusionAltar;

/// <summary>
/// Contributes to <see cref="CEGetKnowledgeEffectEvent"/>: describes which infusion altar recipes
/// a piece of knowledge unlocks.
/// </summary>
public sealed partial class CEInfusionAltarRecipeKnowledgeSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEGetKnowledgeEffectEvent>(OnGetKnowledgeEffect);
    }

    private void OnGetKnowledgeEffect(ref CEGetKnowledgeEffectEvent args)
    {
        var items = new List<string>();

        foreach (var recipe in _proto.EnumeratePrototypes<CEInfusionAltarRecipePrototype>())
        {
            if (recipe.RequiredKnowledge is not { } required || required != args.Knowledge)
                continue;

            if (!_proto.TryIndex(recipe.Result, out var result))
                continue;

            items.Add(result.Name);
        }

        if (items.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.Append(Loc.GetString("ce-knowledge-effect-infusion-altar-header"));
        foreach (var item in items)
        {
            sb.Append('\n');
            sb.Append(Loc.GetString("ce-knowledge-effect-list-item", ("item", item)));
        }

        args.Effects.Add(sb.ToString());
    }
}
