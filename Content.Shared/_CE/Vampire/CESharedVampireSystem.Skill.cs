using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared._CE.Skill.Restrictions;
using Content.Shared._CE.Vampire.Components;
using Content.Shared.Actions;
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

    private void OnGetVampireVerbs(Entity<CEVampireComponent> minorVampire, ref GetVerbsEvent<Verb> args)
    {
        var majorEnt = args.User;
        if (minorVampire.Owner == majorEnt)
            return;

        if (!TryComp<CESkillStorageComponent>(minorVampire, out var minorSkillStorage))
            return;

        if (!TryComp<CEVampireComponent>(majorEnt, out var majorVampComp))
            return;

        if (!majorVampComp.HigherVampire) //Only higher vampires can teach.
            return;

        if (minorVampire.Comp.HigherVampire) //We cant teach higher vampires.
            return;

        var skillPoints = minorSkillStorage.SkillPoints;
        if (!skillPoints.TryGetValue(minorVampire.Comp.SkillPointProto, out var points))
            return;

        foreach (var skill in _skill.GetLearnableSkills(minorVampire.Owner, false, false))
        {
            if (!Proto.Resolve(skill, out var resolvedSkill))
                continue;

            if (resolvedSkill.Tree.Id != minorVampire.Comp.SkillTreeProto.Id)
                continue;

            //Custom restrictions check: we wanna check all restrictions except HigherVampire one
            var reqPass = true;
            foreach (var req in resolvedSkill.Restrictions)
            {
                if (req is HigherVampire)
                    continue;

                if (!req.Check(EntityManager, minorVampire))
                {
                    reqPass = false;
                    break;
                }
            }

            if (!reqPass)
                continue;

            var v = new Verb
            {
                Icon = resolvedSkill.Icon,
                Category = VerbCategory.CEVampireLearn,
                Text = $"{_skill.GetSkillName(skill)} [{resolvedSkill.LearnCost}/{points.Max - points.Sum}]",
                Impact = LogImpact.High,
                DoContactInteraction = true,
                Disabled = points.Sum + resolvedSkill.LearnCost > points.Max,
                Act = () =>
                {
                    var doAfter = new DoAfterArgs(EntityManager,
                        majorEnt,
                        1f,
                        new CEVampireTeachingDoAfterEvent(skill),
                        majorEnt,
                        minorVampire);
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
/// Called when a higher vampire attempts to teach younger vampires skills.
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

public sealed partial class CETransformIntoVampireActionEvent : EntityTargetActionEvent;
