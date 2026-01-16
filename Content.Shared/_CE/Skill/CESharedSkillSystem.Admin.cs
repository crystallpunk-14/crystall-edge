using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill;

public abstract partial class CESharedSkillSystem
{

    private IEnumerable<CESkillPrototype>? _allSkills;
    private IEnumerable<CESkillTreePrototype>? _allTrees;
    private void InitializeAdmin()
    {
        SubscribeLocalEvent<CESkillStorageComponent, GetVerbsEvent<Verb>>(OnGetAdminVerbs);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReloaded);

        UpdateCachedSkill();
    }

    private void OnPrototypeReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<CESkillPrototype>())
            return;

        UpdateCachedSkill();
    }

    private void UpdateCachedSkill()
    {
        _allSkills = _proto.EnumeratePrototypes<CESkillPrototype>();
        _allTrees = _proto.EnumeratePrototypes<CESkillTreePrototype>();
    }


    private void OnGetAdminVerbs(Entity<CESkillStorageComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!_admin.HasAdminFlag(args.User, AdminFlags.Admin))
            return;

        if (_allSkills is null || _allTrees is null)
            return;

        var target = args.Target;

        //Reset/Remove All Skills
        args.Verbs.Add(new Verb
        {
            Text = "Reset skills",
            Message = "Remove all learned skills",
            Category = VerbCategory.Debug,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/_CE/Interface/Misc/reroll.rsi"), "reroll"),
            Act = () =>
            {
                TryResetSkills(target);
            },
        });
    }
}
