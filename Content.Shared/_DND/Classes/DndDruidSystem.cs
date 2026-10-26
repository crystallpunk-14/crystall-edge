using Content.Shared._DND.Abilities;
using Content.Shared._DND.Features;
using Content.Shared._DND.Spells;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._DND.Classes;

public sealed partial class DndDruidSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnSign(EntityUid uid, DndDruidicSignComponent sign, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (HasComp<DndDruidicComponent>(args.Examiner))
        {
            if (sign.Message != "")
                args.PushMarkup(sign.Message);

            return;
        }

        if (!TryComp<DndAbilitiesComponent>(args.Examiner, out var abilities))
            return;

        var bonus = DndAbilityScores.Modifier(abilities.Intelligence);
        if (TryComp<DndTrainingComponent>(args.Examiner, out var training))
        {
            foreach (var skill in training.Skills)
            {
                if (skill.Id == "Investigation")
                    bonus += abilities.ProficiencyBonus;
            }
        }

        bonus += MysticBonus(args.Examiner, "Investigation");
        var roll = _random.Next(1, 21) + bonus;
        if (roll >= DndDruidRules.SignDifficultyClass)
            args.PushMarkup(Loc.GetString("dnd-druidic-noticed"));
    }

    [SubscribeLocalEvent]
    private void OnCreature(EntityUid uid, MobStateComponent component, ref ExaminedEvent args)
    {
        if (!TryComp<DndSpeakWithAnimalsComponent>(args.Examiner, out var speak) || _timing.CurTime >= speak.Until)
            return;

        if (HasComp<DndClassComponent>(uid))
            return;

        args.PushMarkup(Loc.GetString("dnd-spell-understand-animal"));
    }

    private int MysticBonus(EntityUid uid, string skillId)
    {
        if (!TryComp<DndThaumaturgeComponent>(uid, out var mystic))
            return 0;

        if (skillId == mystic.FirstSkillId || skillId == mystic.SecondSkillId)
            return mystic.Bonus;

        return 0;
    }
}
