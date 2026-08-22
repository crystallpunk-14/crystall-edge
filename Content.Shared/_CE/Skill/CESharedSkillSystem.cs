using System.Linq;
using System.Text;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared.Administration.Managers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Skill;

public abstract partial class CESharedSkillSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private ISharedAdminManager _admin = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CESkillStorageComponent, MapInitEvent>(OnMapInit);

        InitializeAdmin();
        InitializeScanning();
        InitializeRead();
        InitializePen();
    }

    private void OnMapInit(Entity<CESkillStorageComponent> ent, ref MapInitEvent args)
    {
        //If at initialization we have any skill records, we automatically apply their effects to this entity

        var learned = ent.Comp.LearnedSkills.ToList();
        ent.Comp.LearnedSkills.Clear();

        foreach (var skill in learned)
        {
            TryAddSkill(ent.Owner, skill, ent.Comp, force: true);
        }
    }

    /// <summary>
    /// Directly adds the skill to the player. Checks <see cref="CESkillPrototype.Conditions"/> via
    /// <see cref="SkillConditionsMet"/> unless <paramref name="force"/> is true - this is the single
    /// authoritative checkpoint for whether a skill can be granted, callers no longer need to
    /// pre-check conditions themselves.
    /// </summary>
    public bool TryAddSkill(EntityUid target,
        ProtoId<CESkillPrototype> skill,
        CESkillStorageComponent? component = null,
        bool force = false)
    {
        if (!Resolve(target, ref component, false))
            return false;

        if (component.LearnedSkills.Contains(skill))
            return false;

        if (!_proto.Resolve(skill, out var indexedSkill))
            return false;

        if (!force && !SkillConditionsMet(target, skill))
            return false;

        indexedSkill.Effect?.AddSkill(EntityManager, target);

        component.LearnedSkills.Add(skill);
        Dirty(target, component);

        var learnEv = new CESkillLearnedEvent(skill, target);
        RaiseLocalEvent(target, ref learnEv);

        return true;
    }

    /// <summary>
    ///  Removes the skill from the player, bypassing any checks.
    /// </summary>
    public bool TryRemoveSkill(EntityUid target,
        ProtoId<CESkillPrototype> skill,
        CESkillStorageComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return false;

        if (!component.LearnedSkills.Remove(skill))
            return false;

        if (!_proto.Resolve(skill, out var indexedSkill))
            return false;

        indexedSkill.Effect?.RemoveSkill(EntityManager, target);

        Dirty(target, component);
        return true;
    }

    /// <summary>
    ///  Checks if the player has the skill.
    /// </summary>
    public bool HaveSkill(EntityUid target,
        ProtoId<CESkillPrototype> skill,
        CESkillStorageComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return false;

        return component.LearnedSkills.Contains(skill);
    }

    /// <summary>
    ///  Helper function to get the skill name for a given skill prototype.
    /// </summary>
    public string GetSkillName(ProtoId<CESkillPrototype> skill)
    {
        if (!_proto.Resolve(skill, out var indexedSkill))
            return skill.Id;

        if (indexedSkill.NameOverride is not null)
            return Loc.GetString(indexedSkill.NameOverride);

        if (GetSkillPreviewEntityProto(indexedSkill) is { } preview)
            return preview.Name;

        var name = indexedSkill.Effect?.GetName(EntityManager, _proto);
        if (name != null)
            return name;

        return skill.Id;
    }

    /// <summary>
    ///  Helper function to get the skill's flavor description for a given skill prototype.
    /// </summary>
    public string GetSkillDescription(ProtoId<CESkillPrototype> skill)
    {
        if (!_proto.Resolve(skill, out var indexedSkill))
            return string.Empty;

        if (indexedSkill.DescOverride is not null)
            return Loc.GetString(indexedSkill.DescOverride);

        if (GetSkillPreviewEntityProto(indexedSkill) is { } preview)
            return preview.Description;

        return string.Empty;
    }

    public SpriteSpecifier? GetSkillIcon(ProtoId<CESkillPrototype> skill)
    {
        if (!_proto.Resolve(skill, out var indexedSkill))
            return null;

        if (indexedSkill.IconOverride is not null)
            return indexedSkill.IconOverride;

        return indexedSkill.Effect?.GetIcon(EntityManager, _proto);
    }

    /// <summary>
    /// Entity prototype to render a preview of, or null if <see cref="GetSkillIcon"/> should be
    /// used instead (a flat icon takes priority over a multi-layer entity preview).
    /// </summary>
    public EntityPrototype? GetSkillPreviewEntity(ProtoId<CESkillPrototype> skill)
    {
        if (!_proto.Resolve(skill, out var indexedSkill))
            return null;

        return indexedSkill.IconOverride is null ? GetSkillPreviewEntityProto(indexedSkill) : null;
    }

    private EntityPrototype? GetSkillPreviewEntityProto(CESkillPrototype skill)
    {
        return skill.PreviewEntity is { } previewId && _proto.TryIndex(previewId, out var indexedProto)
            ? indexedProto
            : null;
    }

    /// <summary>
    /// Builds a human-readable description of everything learning a skill does: its own
    /// <see cref="CESkillEffect.GetDescription"/> (if any), plus a line from every other
    /// skill-aware system (recipes, achievements, ...) that gates something behind it, collected by
    /// raising <see cref="CEGetSkillEffectEvent"/>. Returns an empty string if nothing responded.
    /// </summary>
    public string GetSkillEffectDescription(ProtoId<CESkillPrototype> skill)
    {
        var sb = new StringBuilder();

        if (_proto.Resolve(skill, out var indexedSkill) && indexedSkill.Effect is not null)
            sb.Append(indexedSkill.Effect.GetDescription(EntityManager, _proto, skill));

        var effects = new List<string>();
        var ev = new CEGetSkillEffectEvent(skill, effects);
        RaiseLocalEvent(ref ev);

        foreach (var effect in effects)
        {
            if (sb.Length > 0)
                sb.Append('\n');

            sb.Append(effect);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Whether <paramref name="actor"/> currently satisfies <paramref name="skill"/>'s
    /// <see cref="CESkillPrototype.Conditions"/>. Does not check whether it is already known.
    /// </summary>
    public bool SkillConditionsMet(EntityUid actor, ProtoId<CESkillPrototype> skill)
    {
        if (!_proto.Resolve(skill, out var indexedSkill))
            return false;

        var args = new CEEntityEffectArgs(EntityManager, actor, null, Angle.Zero, 0f, actor, null);
        return indexedSkill.Conditions.All(condition => condition.Passes(args));
    }

    /// <summary>
    ///  Helper function to reset all learned skills.
    /// </summary>
    public bool TryResetSkills(Entity<CESkillStorageComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        for (var i = ent.Comp.LearnedSkills.Count - 1; i >= 0; i--)
        {
            TryRemoveSkill(ent, ent.Comp.LearnedSkills[i], ent.Comp);
        }

        return true;
    }
}

[ByRefEvent]
public record struct CESkillLearnedEvent(ProtoId<CESkillPrototype> Skill, EntityUid User);

/// <summary>
/// Broadcast query raised by <see cref="CESharedSkillSystem.GetSkillEffectDescription"/> to collect
/// what a skill unlocks. Every system that gates something behind a skill (recipes, achievements,
/// etc.) should subscribe and append its own description to <see cref="Effects"/> when it
/// recognizes <see cref="Skill"/>.
/// </summary>
[ByRefEvent]
public readonly record struct CEGetSkillEffectEvent(ProtoId<CESkillPrototype> Skill, List<string> Effects);

/// <summary>
/// DoAfter fired when reading a <see cref="Components.CESkillBookComponent"/> item finishes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CESkillReadDoAfterEvent : SimpleDoAfterEvent;
