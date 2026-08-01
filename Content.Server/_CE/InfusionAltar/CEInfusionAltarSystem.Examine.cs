using Content.Shared._CE.InfusionAltar.Components;
using Content.Shared._CE.MagicVision.Components;
using Content.Shared.Examine;

namespace Content.Server._CE.InfusionAltar;

public sealed partial class CEInfusionAltarSystem
{
    private void InitExamine()
    {
        SubscribeLocalEvent<CEInfusionAltarComponent, ExaminedEvent>(OnAltarExamined);
    }

    /// <summary>
    /// Shows the altar's instability, stabilization and (if a ritual is in progress) craft progress to
    /// examiners currently perceiving with magic vision (e.g. thaumaturgy goggles) - this state isn't
    /// otherwise visible without it.
    /// </summary>
    private void OnAltarExamined(Entity<CEInfusionAltarComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !HasComp<CEMagicVisionComponent>(args.Examiner))
            return;

        var altar = ent.Comp;

        var instabilityPercent = (int)MathF.Round(altar.Instability / altar.MaxInstability * 100f);
        args.PushMarkup(Loc.GetString("ce-infusion-altar-examine-instability", ("percent", instabilityPercent)));

        var stabilizationPercent = (int)MathF.Round((1f - altar.StabilizerFactor) * 100f);
        args.PushMarkup(Loc.GetString("ce-infusion-altar-examine-stabilizers", ("percent", stabilizationPercent)));

        if (altar.AttemptingRecipe is { } recipeId
            && _proto.TryIndex(recipeId, out var recipe)
            && recipe.RitualDuration > TimeSpan.Zero)
        {
            var progressPercent = (int)MathF.Round((float)(altar.RitualProgress / recipe.RitualDuration) * 100f);
            args.PushMarkup(Loc.GetString("ce-infusion-altar-examine-progress", ("percent", progressPercent)));
        }
    }
}
