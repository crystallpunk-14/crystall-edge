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
        SubscribeLocalEvent<CEActionSkillPointCostComponent, ExaminedEvent>(OnSkillPointCostExamined);

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

    private void OnSkillPointCostExamined(Entity<CEActionSkillPointCostComponent> ent, ref ExaminedEvent args)
    {
        if (!_proto.Resolve(ent.Comp.SkillPoint, out var indexedSkillPoint))
            return;

        args.PushMarkup($"{Loc.GetString("ce-magic-skillpointcost", ("name", Loc.GetString(indexedSkillPoint.Name)), ("count", ent.Comp.Count))}", priority: 9);
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
        args.PushMarkup(Loc.GetString("ce-magic-material-aspect") + " " + ent.Comp.Requirement.GetRequirementTitle(_proto));
    }
    private void OnMusicExamined(Entity<CEActionRequiredMusicToolComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ce-magic-music-aspect"));
    }

    private void OnMobStateExamined(Entity<CEActionTargetMobStatusRequiredComponent> ent, ref ExaminedEvent args)
    {
        var states = "";
        foreach (var state in ent.Comp.AllowedStates)
        {
            if (states.Length > 0)
                states += ", ";

            switch (state)
            {
                case MobState.Alive:
                    states += Loc.GetString("ce-magic-spell-target-mob-state-live");
                    break;
                case MobState.Dead:
                    states += Loc.GetString("ce-magic-spell-target-mob-state-dead");
                    break;
                case MobState.Critical:
                    states += Loc.GetString("ce-magic-spell-target-mob-state-critical");
                    break;
            }
        }

        args.PushMarkup(Loc.GetString("ce-magic-spell-target-mob-state", ("state", states)));
    }
}
