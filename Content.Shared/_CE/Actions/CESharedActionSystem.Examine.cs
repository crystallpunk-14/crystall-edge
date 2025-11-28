using System.Linq;
using Content.Shared._CE.Actions.Components;
using Content.Shared.Examine;
using Content.Shared.Mobs;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem
{
    private void InitializeExamine()
    {
        SubscribeLocalEvent<CEActionManaCostComponent, ExaminedEvent>(OnManacostExamined);
        SubscribeLocalEvent<CEActionStaminaCostComponent, ExaminedEvent>(OnStaminaCostExamined);

        SubscribeLocalEvent<CEActionSpeakingComponent, ExaminedEvent>(OnVerbalExamined);
        SubscribeLocalEvent<CEActionFreeHandsRequiredComponent, ExaminedEvent>(OnSomaticExamined);
        SubscribeLocalEvent<CEActionMaterialCostComponent, ExaminedEvent>(OnMaterialExamined);
        SubscribeLocalEvent<CEActionRequiredMusicToolComponent, ExaminedEvent>(OnMusicExamined);
        SubscribeLocalEvent<CEActionTargetMobStatusRequiredComponent, ExaminedEvent>(OnMobStateExamined);
    }

    private void OnManacostExamined(Entity<CEActionManaCostComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup($"{Loc.GetString("ce-magic-manacost")}: [color=#5da9e8]{ent.Comp.ManaCost}[/color]", priority: 9);
    }

    private void OnStaminaCostExamined(Entity<CEActionStaminaCostComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup($"{Loc.GetString("ce-magic-staminacost")}: [color=#3fba54]{ent.Comp.Stamina}[/color]", priority: 9);
    }

    private void OnVerbalExamined(Entity<CEActionSpeakingComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ce-magic-verbal-aspect"), 8);
    }

    private void OnSomaticExamined(Entity<CEActionFreeHandsRequiredComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ce-magic-somatic-aspect") + " " + ent.Comp.FreeHandRequired, 8);
    }

    private void OnMaterialExamined(Entity<CEActionMaterialCostComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Requirement is not null)
            args.PushMarkup(Loc.GetString("ce-magic-material-aspect") + " " + ent.Comp.Requirement.GetRequirementTitle(_proto));
    }
    private void OnMusicExamined(Entity<CEActionRequiredMusicToolComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ce-magic-music-aspect"));
    }

    private void OnMobStateExamined(Entity<CEActionTargetMobStatusRequiredComponent> ent, ref ExaminedEvent args)
    {
        var states = string.Join(", ",
            ent.Comp.AllowedStates.Select(state => state switch
        {
            MobState.Alive => Loc.GetString("ce-magic-spell-target-mob-state-live"),
            MobState.Dead => Loc.GetString("ce-magic-spell-target-mob-state-dead"),
            MobState.Critical => Loc.GetString("ce-magic-spell-target-mob-state-critical")
        }));

        args.PushMarkup(Loc.GetString("ce-magic-spell-target-mob-state", ("state", states)));
    }
}
