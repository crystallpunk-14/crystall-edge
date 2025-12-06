using System.Linq;
using System.Text;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared._CE.Skill.Restrictions;
using Content.Shared.Administration.Managers;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Skill;

public abstract partial class CESharedSkillSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private EntityQuery<CESkillStorageComponent> _skillStorageQuery;

    public override void Initialize()
    {
        base.Initialize();

        _skillStorageQuery = GetEntityQuery<CESkillStorageComponent>();

        SubscribeLocalEvent<CESkillStorageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CESkillPointConsumableComponent, UseInHandEvent>(OnInteracted);

        InitializeAdmin();
        InitializeChecks();
        InitializeScanning();
    }

    private void OnInteracted(Entity<CESkillPointConsumableComponent> ent, ref UseInHandEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ent.Comp.Whitelist is null || !_whitelist.IsValid(ent.Comp.Whitelist, args.User))
            return;

        if (_net.IsServer)
        {
            var collect = ent.Comp.Volume;

            if (TryComp<StackComponent>(ent, out var stack))
                collect *= stack.Count;

            TryAddSkillPoints(args.User, ent.Comp.PointType, collect);
        }

        var position = Transform(ent).Coordinates;

        //Client VFX
        if (_net.IsClient)
            SpawnAtPosition(ent.Comp.ConsumeEffect, position);

        _audio.PlayPredicted(ent.Comp.ConsumeSound, position, args.User);

        PredictedQueueDel(ent.Owner);
    }

    private void OnMapInit(Entity<CESkillStorageComponent> ent, ref MapInitEvent args)
    {
        //If at initialization we have any skill records, we automatically give them to this entity

        var free = ent.Comp.FreeLearnedSkills.ToList();
        var learned = ent.Comp.LearnedSkills.ToList();

        ent.Comp.FreeLearnedSkills.Clear();
        ent.Comp.LearnedSkills.Clear();

        foreach (var skill in free)
        {
            TryAddSkill(ent.Owner, skill, ent.Comp, true);
        }

        foreach (var skill in learned)
        {
            TryAddSkill(ent.Owner, skill, ent.Comp);
        }
    }

    /// <summary>
    ///  Adds a skill tree to the player, allowing them to learn skills from it.
    /// </summary>
    public void AddSkillTree(EntityUid target,
        ProtoId<CESkillTreePrototype> tree,
        CESkillStorageComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return;

        component.AvailableSkillTrees.Add(tree);
        DirtyField(target, component, nameof(CESkillStorageComponent.AvailableSkillTrees));
    }

    public void RemoveSkillTree(EntityUid target,
        ProtoId<CESkillTreePrototype> tree,
        CESkillStorageComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return;

        component.AvailableSkillTrees.Remove(tree);
        DirtyField(target, component, nameof(CESkillStorageComponent.AvailableSkillTrees));
    }

    /// <summary>
    /// Directly adds the skill to the player, bypassing any checks.
    /// </summary>
    public bool TryAddSkill(EntityUid target,
        ProtoId<CESkillPrototype> skill,
        CESkillStorageComponent? component = null,
        bool free = false)
    {
        if (!Resolve(target, ref component, false))
            return false;

        if (component.LearnedSkills.Contains(skill))
            return false;

        if (!_proto.Resolve(skill, out var indexedSkill))
            return false;

        if (!_proto.Resolve(indexedSkill.Tree, out var indexedTree))
            return false;

        foreach (var effect in indexedSkill.Effects)
        {
            effect.AddSkill(EntityManager, target);
        }

        if (free)
            component.FreeLearnedSkills.Add(skill);
        else
        {
            if (component.SkillPoints.TryGetValue(indexedTree.SkillType, out var skillContainer))
            {
                skillContainer.Sum += indexedSkill.LearnCost;
            }
        }

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

        if (!_proto.Resolve(indexedSkill.Tree, out var indexedTree))
            return false;

        foreach (var effect in indexedSkill.Effects)
        {
            effect.RemoveSkill(EntityManager, target);
        }

        if (!component.FreeLearnedSkills.Remove(skill) &&
            component.SkillPoints.TryGetValue(indexedTree.SkillType, out var skillContainer))
        {
            skillContainer.Sum -= indexedSkill.LearnCost;
        }

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

    public bool HaveFreeSkill(EntityUid target,
        ProtoId<CESkillPrototype> skill,
        CESkillStorageComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return false;

        return component.FreeLearnedSkills.Contains(skill);
    }

    /// <summary>
    ///  Checks if the player can learn the specified skill.
    /// </summary>
    public bool CanLearnSkill(EntityUid target,
        ProtoId<CESkillPrototype> skill,
        CESkillStorageComponent? component = null)
    {
        if (!_proto.Resolve(skill, out var indexedSkill))
            return false;

        return CanLearnSkill(target, indexedSkill, component);
    }

    /// <summary>
    ///  Checks if the player can learn the specified skill.
    /// </summary>
    public bool CanLearnSkill(EntityUid target,
        CESkillPrototype skill,
        CESkillStorageComponent? component = null,
        bool checkSkillPoints = true,
        bool checkRestrictions = true)
    {
        if (!Resolve(target, ref component, false))
            return false;

        if (!_proto.Resolve(skill.Tree, out var indexedTree))
            return false;

        //Already learned
        if (HaveSkill(target, skill, component))
            return false;

        //Check if the skill is in the available skill trees
        if (!component.AvailableSkillTrees.Contains(skill.Tree))
            return false;

        //Check skill points
        if (checkSkillPoints)
        {
            if (!component.SkillPoints.TryGetValue(indexedTree.SkillType, out var skillContainer))
                return false;

            if (skillContainer.Sum + skill.LearnCost > skillContainer.Max)
                return false;
        }

        if (checkRestrictions)
        {
            //Restrictions check
            foreach (var req in skill.Restrictions)
            {
                if (!req.Check(EntityManager, target))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    ///  Tries to learn the specified skill for the player.
    /// </summary>
    public bool TryLearnSkill(EntityUid target,
        ProtoId<CESkillPrototype> skill,
        CESkillStorageComponent? component = null)
    {
        if (!Resolve(target, ref component, false))
            return false;

        if (!CanLearnSkill(target, skill, component))
            return false;

        if (!TryAddSkill(target, skill, component))
            return false;

        return true;
    }

    /// <summary>
    ///  Helper function to get the skill name for a given skill prototype.
    /// </summary>
    public string GetSkillName(ProtoId<CESkillPrototype> skill)
    {
        if (!_proto.Resolve(skill, out var indexedSkill))
            return string.Empty;

        if (indexedSkill.Name is not null)
            return Loc.GetString(indexedSkill.Name);

        foreach (var effect in indexedSkill.Effects)
        {
            var name = effect.GetName(EntityManager, _proto);
            if (name != null)
                return name;
        }

        return string.Empty;
    }

    /// <summary>
    ///  Helper function to get the skill description for a given skill prototype.
    /// </summary>
    public string GetSkillDescription(ProtoId<CESkillPrototype> skill)
    {
        if (!_proto.Resolve(skill, out var indexedSkill))
            return string.Empty;

        var sb = new StringBuilder();

        if (indexedSkill.Desc is not null)
            sb.Append(Loc.GetString(indexedSkill.Desc));

        foreach (var effect in indexedSkill.Effects)
        {
            sb.Append(effect.GetDescription(EntityManager, _proto, skill) + "\n");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Obtaining all learned skills that are not prerequisites for other skills of this creature
    /// </summary>
    public HashSet<ProtoId<CESkillPrototype>> GetFrontierSkills(Entity<CESkillStorageComponent?> ent)
    {
        var skills = new HashSet<ProtoId<CESkillPrototype>>();
        if (!Resolve(ent, ref ent.Comp, false))
            return skills;

        var frontier = ent.Comp.LearnedSkills.ToHashSet();
        foreach (var skill in ent.Comp.LearnedSkills)
        {
            if (!_proto.Resolve(skill, out var indexedSkill))
                continue;

            if (HaveFreeSkill(ent, skill))
                continue;

            foreach (var req in indexedSkill.Restrictions)
            {
                if (req is NeedPrerequisite prerequisite && frontier.Contains(prerequisite.Prerequisite))
                    frontier.Remove(prerequisite.Prerequisite);
            }
        }

        return frontier;
    }

    /// <summary>
    /// Returns a list of all skills the entity can currently learn.
    /// </summary>
    public HashSet<ProtoId<CESkillPrototype>> GetLearnableSkills(Entity<CESkillStorageComponent?> ent,
        bool checkSkillPoints = true,
        bool checkRestrictions = true)
    {
        var skills = new HashSet<ProtoId<CESkillPrototype>>();

        if (!Resolve(ent, ref ent.Comp, false))
            return skills;

        foreach (var skill in _proto.EnumeratePrototypes<CESkillPrototype>())
        {
            if (ent.Comp.LearnedSkills.Contains(skill))
                continue;

            if (!CanLearnSkill(ent.Owner, skill, ent.Comp, checkSkillPoints, checkRestrictions))
                continue;

            skills.Add(skill);
        }

        return skills;
    }

    /// <summary>
    ///  Helper function to reset skills to only learned skills
    /// </summary>
    public bool TryResetSkills(Entity<CESkillStorageComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        for (var i = ent.Comp.LearnedSkills.Count - 1; i >= 0; i--)
        {
            if (HaveFreeSkill(ent, ent.Comp.LearnedSkills[i], ent.Comp))
            {
                continue;
            }

            TryRemoveSkill(ent, ent.Comp.LearnedSkills[i], ent.Comp);
        }

        return true;
    }

    /// <summary>
    /// Increases the number of skill points for a character, limited to a certain amount.
    /// </summary>
    public bool TryAddSkillPoints(Entity<CESkillStorageComponent?> ent,
        ProtoId<CESkillPointPrototype> type,
        FixedPoint2 points,
        FixedPoint2? limit = null,
        bool silent = false)
    {
        if (points <= 0)
            return true;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!_proto.Resolve(type, out var indexedType))
            return false;

        if (!ent.Comp.SkillPoints.TryGetValue(type, out var skillContainer))
        {
            skillContainer = new CESkillPointContainerEntry();
            ent.Comp.SkillPoints[type] = skillContainer;
        }

        skillContainer.Max = limit is not null
            ? FixedPoint2.Min(skillContainer.Max + points, limit.Value)
            : skillContainer.Max + points;

        DirtyField(ent, ent.Comp, nameof(CESkillStorageComponent.SkillPoints));

        if (indexedType.GetPointPopup is not null && !silent && _timing.IsFirstTimePredicted)
            _popup.PopupClient(Loc.GetString(indexedType.GetPointPopup, ("count", points)), ent, ent);

        return true;
    }

    /// <summary>
    /// Removes skill points. If a character has accumulated skills exceeding the new memory limit, random skills will be removed.
    /// </summary>
    public bool TryRemoveSkillPoints(Entity<CESkillStorageComponent?> ent,
        ProtoId<CESkillPointPrototype> type,
        FixedPoint2 points,
        bool silent = false)
    {
        if (points <= 0)
            return true;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!_proto.Resolve(type, out var indexedType))
            return false;

        if (!ent.Comp.SkillPoints.TryGetValue(type, out var skillContainer))
            return false;

        skillContainer.Max = FixedPoint2.Max(skillContainer.Max - points, 0);
        Dirty(ent);

        if (indexedType.LosePointPopup is not null && !silent && _timing.IsFirstTimePredicted)
            _popup.PopupClient(Loc.GetString(indexedType.LosePointPopup, ("count", points)), ent, ent);

        while (skillContainer.Sum > skillContainer.Max)
        {
            var frontier = GetFrontierSkills((ent, ent.Comp));
            if (frontier.Count == 0)
                break;

            //Randomly remove one of the frontier skills
            var skill = _random.Pick(frontier);
            TryRemoveSkill(ent, skill, ent.Comp);
        }

        return true;
    }
}

[ByRefEvent]
public record struct CESkillLearnedEvent(ProtoId<CESkillPrototype> Skill, EntityUid User);
