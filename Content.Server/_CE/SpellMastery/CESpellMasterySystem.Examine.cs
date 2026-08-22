using Content.Shared._CE.MagicVision.Components;
using Content.Shared._CE.SpellMastery.Components;
using Content.Shared.Examine;

namespace Content.Server._CE.SpellMastery;

// CrystallEdge: rough copy of Content.Server._CE.InfusionAltar.CEInfusionAltarSystem.Examine,
// not yet cleaned up/unified.

public sealed partial class CESpellMasterySystem
{
    private void InitExamine()
    {
        SubscribeLocalEvent<CESpellMasteryAltarComponent, ExaminedEvent>(OnAltarExamined);
    }

    /// <summary>
    /// Shows the altar's instability, stabilization and (if a ritual is in progress) training progress
    /// to examiners currently perceiving with magic vision (e.g. thaumaturgy goggles) - this state
    /// isn't otherwise visible without it.
    /// </summary>
    private void OnAltarExamined(Entity<CESpellMasteryAltarComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !HasComp<CEMagicVisionComponent>(args.Examiner))
            return;

        var altar = ent.Comp;

        // Reuses the infusion altar's existing loc strings as-is - same "chaos/ritual progress"
        // framing applies here.
        var instabilityPercent = (int)MathF.Round(altar.Instability / altar.MaxInstability * 100f);
        args.PushMarkup(Loc.GetString("ce-infusion-altar-examine-instability", ("percent", instabilityPercent)));

        if (altar.AttemptingRecipe is { } recipeId
            && _proto.TryIndex(recipeId, out var recipe)
            && recipe.RitualDuration > TimeSpan.Zero)
        {
            var progressPercent = (int)MathF.Round((float)(altar.RitualProgress / recipe.RitualDuration) * 100f);
            args.PushMarkup(Loc.GetString("ce-infusion-altar-examine-progress", ("percent", progressPercent)));
        }
    }
}
