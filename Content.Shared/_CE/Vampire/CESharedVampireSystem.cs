using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared._CE.Trading.Prototypes;
using Content.Shared._CE.Trading.Systems;
using Content.Shared._CE.Vampire.Components;
using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Vampire;

public abstract partial class CESharedVampireSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CESharedSkillSystem _skill = default!;
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly CESharedTradingPlatformSystem _trade = default!;

    private readonly ProtoId<CESkillPointPrototype> _skillPointType = "Blood";
    private readonly ProtoId<CESkillPointPrototype> _memorySkillPointType = "Memory";
    private readonly ProtoId<CETradingFactionPrototype> _tradeFaction = "VampireMarket";

    public override void Initialize()
    {
        base.Initialize();
        InitializeSpell();
        InitializeSkills();

        SubscribeLocalEvent<CEVampireComponent, MapInitEvent>(OnVampireInit);
        SubscribeLocalEvent<CEVampireComponent, ComponentRemove>(OnVampireRemove);

        SubscribeLocalEvent<CEVampireComponent, CEToggleVampireVisualsAction>(OnToggleVisuals);
        SubscribeLocalEvent<CEVampireComponent, CEVampireToggleVisualsDoAfter>(OnToggleDoAfter);

        SubscribeLocalEvent<CEVampireVisualsComponent, ComponentInit>(OnVampireVisualsInit);
        SubscribeLocalEvent<CEVampireVisualsComponent, ComponentShutdown>(OnVampireVisualsShutdown);
        SubscribeLocalEvent<CEVampireVisualsComponent, ExaminedEvent>(OnVampireExamine);

        SubscribeLocalEvent<CEVampireEssenceHolderComponent, ExaminedEvent>(OnEssenceHolderExamined);
    }

    private void OnEssenceHolderExamined(Entity<CEVampireEssenceHolderComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<CEShowVampireEssenceComponent>(args.Examiner))
            return;

        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("ce-vampire-essence-holder-examine", ("essence", ent.Comp.Essence)));
    }

    protected virtual void OnVampireInit(Entity<CEVampireComponent> ent, ref MapInitEvent args)
    {
        //Bloodstream
        _bloodstream.ChangeBloodReagent(ent.Owner, ent.Comp.NewBloodReagent);

        //Actions
        foreach (var proto in ent.Comp.ActionsProto)
        {
            EntityUid? newAction = null;
            _action.AddAction(ent, ref newAction, proto);
        }

        //Skill tree
        _skill.TryAddSkillPoints(ent.Owner, ent.Comp.SkillPointProto, ent.Comp.SkillPointCount, silent: true);
        _skill.AddSkillTree(ent, ent.Comp.SkillTreeProto);

        //Skill tree base nerf
        _skill.TryRemoveSkillPoints(ent.Owner, _memorySkillPointType, 2, true);

        //Remove blood essence
        if (TryComp<CEVampireEssenceHolderComponent>(ent, out var essenceHolder))
        {
            essenceHolder.Essence = 0;
            Dirty(ent, essenceHolder);
        }

        //Additional trade faction
        _trade.AddContractToPlayer(ent.Owner, _tradeFaction);
    }

    private void OnVampireRemove(Entity<CEVampireComponent> ent, ref ComponentRemove args)
    {
        RemCompDeferred<CEVampireVisualsComponent>(ent);

        // TODO: Restore original blood reagent when vampire component is removed

        // TODO: Reset metabolism changes when vampire component is removed

        //Actions
        foreach (var action in ent.Comp.Actions)
        {
            _action.RemoveAction(ent.Owner, action);
        }

        //Skill tree
        _skill.RemoveSkillTree(ent, ent.Comp.SkillTreeProto);
        if (TryComp<CESkillStorageComponent>(ent, out var storage))
        {
            foreach (var skill in storage.LearnedSkills)
            {
                if (!Proto.TryIndex(skill, out var indexedSkill))
                    continue;

                if (indexedSkill.Tree == ent.Comp.SkillTreeProto)
                    _skill.TryRemoveSkill(ent, skill);
            }
        }

        _skill.TryRemoveSkillPoints(ent.Owner, ent.Comp.SkillPointProto, ent.Comp.SkillPointCount);
        _skill.TryAddSkillPoints((ent, storage), _memorySkillPointType, 2, null, true);
    }

    private void OnToggleVisuals(Entity<CEVampireComponent> ent, ref CEToggleVampireVisualsAction args)
    {
        if (_timing.IsFirstTimePredicted)
            _jitter.DoJitter(ent, ent.Comp.ToggleVisualsTime, true);

        var doAfterArgs = new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.ToggleVisualsTime,
            new CEVampireToggleVisualsDoAfter(),
            ent)
        {
            Hidden = true,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnToggleDoAfter(Entity<CEVampireComponent> ent, ref CEVampireToggleVisualsDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (HasComp<CEVampireVisualsComponent>(ent))
        {
            RemCompDeferred<CEVampireVisualsComponent>(ent);
        }
        else
        {
            EnsureComp<CEVampireVisualsComponent>(ent);
        }

        args.Handled = true;
    }

    protected virtual void OnVampireVisualsShutdown(Entity<CEVampireVisualsComponent> vampire,
        ref ComponentShutdown args)
    {
        if (!EntityManager.TryGetComponent(vampire, out HumanoidAppearanceComponent? humanoidAppearance))
            return;

        humanoidAppearance.EyeColor = vampire.Comp.OriginalEyesColor;

        Dirty(vampire, humanoidAppearance);
    }

    protected virtual void OnVampireVisualsInit(Entity<CEVampireVisualsComponent> vampire, ref ComponentInit args)
    {
        if (!EntityManager.TryGetComponent(vampire, out HumanoidAppearanceComponent? humanoidAppearance))
            return;

        vampire.Comp.OriginalEyesColor = humanoidAppearance.EyeColor;
        humanoidAppearance.EyeColor = vampire.Comp.EyesColor;

        Dirty(vampire, humanoidAppearance);
    }

    private void OnVampireExamine(Entity<CEVampireVisualsComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ce-vampire-examine"));
    }

    public void GatherEssence(Entity<CEVampireComponent?> vampire,
        Entity<CEVampireEssenceHolderComponent?> victim,
        FixedPoint2 amount)
    {
        if (!Resolve(vampire, ref vampire.Comp, false))
            return;

        if (!Resolve(victim, ref victim.Comp, false))
            return;

        var extractedEssence = MathF.Min(victim.Comp.Essence.Float(), amount.Float());

        if (TryComp<BuckleComponent>(victim, out var buckle)
            && buckle.BuckledTo is not null
            && TryComp<CEVampireAltarComponent>(buckle.BuckledTo, out var altar))
            extractedEssence *= altar.Multiplier;

        if (extractedEssence <= 0)
        {
            _popup.PopupClient(Loc.GetString("ce-vampire-gather-essence-no-left"),
                victim,
                vampire,
                PopupType.SmallCaution);
            return;
        }

        _skill.TryAddSkillPoints(vampire.Owner, _skillPointType, extractedEssence);
        victim.Comp.Essence -= amount;

        Dirty(victim);
    }
}

public sealed partial class CEToggleVampireVisualsAction : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class CEVampireToggleVisualsDoAfter : SimpleDoAfterEvent;

// Appearance Data key
[Serializable, NetSerializable]
public enum VampireClanLevelVisuals : byte
{
    Level,
}
