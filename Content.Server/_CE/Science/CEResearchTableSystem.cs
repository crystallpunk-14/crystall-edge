using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science;

public sealed partial class CEResearchTableSystem : CESharedResearchTableSystem
{
    [Dependency] private CEMagicEssenceSystem _essence = default!;
    [Dependency] private CEScienceSystem _science = default!;
    [Dependency] private CESharedSkillSystem _skill = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private readonly EntProtoId _projectProto = "CEUnselectedDiscoveryProject";

    private static readonly SoundSpecifier ScribbleSound = new SoundCollectionSpecifier("PaperScribbles");
    private static readonly SoundSpecifier SkillLearnedSound = new SoundPathSpecifier("/Audio/_CE/Effects/knowledge_learned.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableMergeAspectsMessage>(OnMergeAspects);
        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableStartResearchMessage>(OnStartResearch);
        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableChooseDiscoveryMessage>(OnChooseDiscovery);
        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTablePlaceAspectMessage>(OnPlaceAspect);
        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableFinishResearchMessage>(OnFinishResearch);
    }

    private void OnMergeAspects(Entity<CEResearchTableComponent> ent, ref CEResearchTableMergeAspectsMessage args)
    {
        if (!_essence.TryGetMergeResult(args.First, args.Second, out var result))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(args.Actor);
        var cost = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> { [args.First] = 1, [args.Second] = 1 };

        if (!_science.TrySpendPoints((args.Actor, data), cost))
            return;

        _science.GrantPoints((args.Actor, data), new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> { [result] = 1 });
    }

    private void OnStartResearch(Entity<CEResearchTableComponent> ent, ref CEResearchTableStartResearchMessage args)
    {
        if (!_proto.TryIndex(args.Area, out var area))
            return;

        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.PaperSlotId) is not { } paper ||
            !HasComp<PaperComponent>(paper))
            return;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.PaperSlotId, out var container))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(args.Actor);
        if (!_science.TrySpendPoints((args.Actor, data), area.Cost))
            return;

        var candidates = _science.DrawOffer(data, args.Area, args.Actor, 3);

        var project = Spawn(_projectProto, Transform(ent.Owner).Coordinates);
        var projectComp = EnsureComp<CEUnselectedDiscoveryProjectComponent>(project);
        projectComp.Player = args.Actor;
        projectComp.Candidates = candidates;
        Dirty(project, projectComp);

        _container.Remove(paper, container);
        Del(paper);

        var authorName = MetaData(args.Actor).EntityName;
        var authorLine = Loc.GetString("ce-skill-book-author", ("name", authorName));
        var baseDescription = MetaData(project).EntityDescription;
        _metaData.SetEntityDescription(project, string.IsNullOrEmpty(baseDescription)
            ? authorLine
            : $"{baseDescription}\n{authorLine}");

        _container.Insert(project, container);

        _audio.PlayPvs(ScribbleSound, ent.Owner);
    }

    private void OnChooseDiscovery(Entity<CEResearchTableComponent> ent, ref CEResearchTableChooseDiscoveryMessage args)
    {
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.PaperSlotId) is not { } item ||
            !TryComp<CEUnselectedDiscoveryProjectComponent>(item, out var draft))
            return;

        if (draft.Player != args.Actor || !draft.Candidates.Contains(args.Discovery))
            return;

        if (!_proto.TryIndex(args.Discovery, out var discovery) ||
            !_proto.HasIndex(discovery.Skill) ||
            !_proto.TryIndex(discovery.Area, out var area))
            return;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.PaperSlotId, out var container))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(args.Actor);
        _science.ReturnUnchosen(data, draft.Candidates, args.Discovery);

        _container.Remove(item, container);
        Del(item);

        var project = Spawn(area.Project, Transform(ent.Owner).Coordinates);
        var projectComp = EnsureComp<CEDiscoveryProjectComponent>(project);
        projectComp.Discovery = args.Discovery;
        projectComp.Tiles = _science.GenerateMap(discovery);
        Dirty(project, projectComp);

        var discoveryName = _skill.GetSkillName(discovery.Skill);
        _metaData.SetEntityName(project, Loc.GetString("ce-discovery-project-name", ("discovery", discoveryName)));

        var description = Loc.GetString("ce-discovery-project-description",
            ("area", Loc.GetString(area.Name)),
            ("discovery", discoveryName));
        var authorLine = Loc.GetString("ce-skill-book-author", ("name", MetaData(args.Actor).EntityName));
        _metaData.SetEntityDescription(project, $"{description}\n{authorLine}");

        _container.Insert(project, container);

        _audio.PlayPvs(ScribbleSound, ent.Owner);
    }

    private void OnPlaceAspect(Entity<CEResearchTableComponent> ent, ref CEResearchTablePlaceAspectMessage args)
    {
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.PaperSlotId) is not { } item ||
            !TryComp<CEDiscoveryProjectComponent>(item, out var project))
            return;

        if (!_proto.TryIndex(project.Discovery, out var discovery) ||
            !_proto.HasIndex(discovery.Skill))
            return;

        if (!CanPlaceAspect(project.Tiles, discovery.Generation.Radius, args.Hex, args.Essence))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(args.Actor);
        var cost = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> { [args.Essence] = 1 };
        if (!_science.TrySpendPoints((args.Actor, data), cost))
            return;

        project.Tiles[args.Hex] = new CEResearchMapTile { Aspect = args.Essence };
        Dirty(item, project);

        _audio.PlayPvs(ScribbleSound, ent.Owner);
    }

    private void OnFinishResearch(Entity<CEResearchTableComponent> ent, ref CEResearchTableFinishResearchMessage args)
    {
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.PaperSlotId) is not { } item ||
            !TryComp<CEDiscoveryProjectComponent>(item, out var project))
            return;

        if (project.Discovery is not { } discoveryId ||
            !_proto.TryIndex(discoveryId, out var discovery) ||
            !_proto.TryIndex(discovery.Skill, out var skill))
            return;

        if (!IsProjectSolved(project.Tiles))
            return;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.PaperSlotId, out var container))
            return;

        // Eligibility was already checked when this discovery was drawn into the offer
        // (DiscoveryRequirementsMet); no need to re-gate a puzzle the player already solved.
        _skill.TryAddSkill(args.Actor, discovery.Skill);

        _container.Remove(item, container);
        Del(item);

        var spawned = Spawn(skill.Book, Transform(ent.Owner).Coordinates);

        // Skill is a required field read during the component's own MapInit handler (which
        // AddComp fires immediately, since the entity is already map-initialized) - it must be
        // set before the component is added, not after via EnsureComp (that handler also takes
        // care of the base name/description).
        AddComp(spawned, new CESkillBookComponent { Skill = discovery.Skill });

        var descLines = new List<string>();

        var effects = _skill.GetSkillEffectDescription(discovery.Skill);
        if (effects.Length > 0)
            descLines.Add(effects);

        var authorName = MetaData(args.Actor).EntityName;
        descLines.Add(Loc.GetString("ce-skill-book-author", ("name", authorName)));
        _metaData.SetEntityDescription(spawned, string.Join("\n", descLines));

        _ui.CloseUi(ent.Owner, CEResearchTableUiKey.Key);

        _audio.PlayPvs(SkillLearnedSound, ent.Owner);
    }
}
