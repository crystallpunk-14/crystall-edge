using System.Text;
using Content.Shared._CE.Skill.Components;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill;

public abstract partial class CESharedSkillSystem
{
    private void InitializeScanning()
    {
        SubscribeLocalEvent<CESkillScannerComponent, CESkillScanEvent>(OnSkillScan);
        SubscribeLocalEvent<CESkillScannerComponent, InventoryRelayedEvent<CESkillScanEvent>>((e, c, ev) => OnSkillScan(e, c, ev.Args));

        SubscribeLocalEvent<CESkillStorageComponent, GetVerbsEvent<ExamineVerb>>(OnExamined);
    }

    private void OnExamined(Entity<CESkillStorageComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        var scanEvent = new CESkillScanEvent();
        RaiseLocalEvent(args.User, scanEvent);

        if (!scanEvent.CanScan)
            return;

        var markup = GetSkillExamine(ent);

        _examine.AddDetailedExamineVerb(
            args,
            ent.Comp,
            markup,
            Loc.GetString("ce-skill-info-title"),
            "/Textures/Interface/students-cap.svg.192dpi.png");
    }

    private FormattedMessage GetSkillExamine(Entity<CESkillStorageComponent> ent)
    {
        var msg = new FormattedMessage();

        var sb = new StringBuilder();

        sb.Append(Loc.GetString("ce-skill-examine-title") + "\n");

        foreach (var skill in ent.Comp.LearnedSkills)
        {
            if (!_proto.Resolve(skill, out var indexedSkill))
                continue;

            if(!_proto.Resolve(indexedSkill.Tree, out var indexedTree))
                continue;

            var skillName = GetSkillName(skill);
            sb.Append($"• [color={indexedTree.Color.ToHex()}]{skillName}[/color]\n");
        }

        //sb.Append($"\n{Loc.GetString("ce-skill-menu-level")} {ent.Comp.SkillsSumExperience}/{ent.Comp.ExperienceMaxCap}\n");
        msg.AddMarkupOrThrow(sb.ToString());
        return msg;
    }

    private void OnSkillScan(EntityUid uid, CESkillScannerComponent component, CESkillScanEvent args)
    {
        args.CanScan = true;
    }
}

public sealed class CESkillScanEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool CanScan;
    public SlotFlags TargetSlots { get; } = SlotFlags.EYES;
}
