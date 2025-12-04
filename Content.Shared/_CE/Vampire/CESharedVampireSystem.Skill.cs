using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared._CE.Skill.Restrictions;
using Content.Shared._CE.Vampire.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Vampire;

public abstract partial class CESharedVampireSystem
{
    /// <summary>
    /// This Partial class is responsible for the mechanics of teaching lower vampires magic using the energy of a higher vampire.
    /// </summary>
    private void InitializeSkills()
    {
        SubscribeLocalEvent<CEVampireComponent, GetVerbsEvent<Verb>>(OnGetVampireVerbs);
        SubscribeLocalEvent<CEVampireComponent, CEVampireTeachingDoAfterEvent>(OnTeachDoAfter);
    }

    private void OnGetVampireVerbs(Entity<CEVampireComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (ent.Owner == args.User)
            return;

        if (!TryComp<CESkillStorageComponent>(args.User, out var skillStorage))
            return;

        if (!TryComp<CEVampireComponent>(args.User, out var vampireExaminer)) //Я остановился на том, что нужно зараты очков у старшего убрать, и дать младшему. Тут хуйня с целями
            return;

        if (!vampireExaminer.HigherVampire) //Only higher vampires can teach.
            return;

        if (ent.Comp.HigherVampire) //We cant teach higher vampires.
            return;

        var skillPoints = skillStorage.SkillPoints;
        if (!skillPoints.TryGetValue(ent.Comp.SkillPointProto, out var points))
            return;

        foreach (var skill in _skill.GetLearnableSkills(ent.Owner, false, false))
        {
            if (!Proto.Resolve(skill, out var resolvedSkill))
                continue;

            if (resolvedSkill.Tree.Id != ent.Comp.SkillTreeProto.Id)
                continue;

            //Custom restrictions check: we wanna check all restrictions except HigherVampire one
            var reqPass = true;
            foreach (var req in resolvedSkill.Restrictions)
            {
                if (req is HigherVampire)
                    continue;

                if (!req.Check(EntityManager, args.Target))
                {
                    reqPass = false;
                    break;
                }
            }

            if (!reqPass)
                continue;

            var user = args.User;
            var target = args.Target;
            var v = new Verb()
            {
                Icon = resolvedSkill.Icon,
                Category = VerbCategory.CEVampireLearn,
                Text = $"{_skill.GetSkillName(skill)} [{resolvedSkill.LearnCost}]",
                Impact = LogImpact.High,
                DoContactInteraction = true,
                Disabled = points.Sum + resolvedSkill.LearnCost > points.Max,
                Act = () =>
                {
                    var doAfter = new DoAfterArgs(EntityManager,
                        user,
                        1f,
                        new CEVampireTeachingDoAfterEvent(skill),
                        user,
                        target);
                    _doAfter.TryStartDoAfter(doAfter);
                },
            };

            args.Verbs.Add(v);
        }
    }

    private void OnTeachDoAfter(Entity<CEVampireComponent> ent, ref CEVampireTeachingDoAfterEvent args)
    {
        if (args.Target is null || args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        _skill.TryAddSkill(args.Target.Value, args.Skill);
    }
}


/// <summary>
/// Called upon the highest vampire when he attempts to teach younger vampires skills
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CEVampireTeachingDoAfterEvent : DoAfterEvent
{
    public ProtoId<CESkillPrototype> Skill;

    public CEVampireTeachingDoAfterEvent(ProtoId<CESkillPrototype> skill)
    {
        Skill = skill;
    }

    public override DoAfterEvent Clone() => this;
}
