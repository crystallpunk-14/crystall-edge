using Content.Shared._CE.Hex;
using Content.Shared._CE.Knowledge;
using Content.Shared._CE.Knowledge.Components;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
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
    [Dependency] private CEKnowledgeSystem _knowledge = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private readonly EntProtoId _projectProto = "CEUnselectedDiscoveryProject";

    private static readonly SoundSpecifier ScribbleSound = new SoundCollectionSpecifier("PaperScribbles");
    private static readonly SoundSpecifier KnowledgeLearnedSound = new SoundPathSpecifier("/Audio/_CE/Effects/knowledge_learned.ogg");

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

        if (!_science.TryGetSingleton(out var science))
            return;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.PaperSlotId, out var container))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(args.Actor);
        if (!_science.TrySpendPoints((args.Actor, data), area.Cost))
            return;

        var candidates = _science.DrawOffer(science, args.Area, args.Actor, 3);

        var project = Spawn(_projectProto, Transform(ent.Owner).Coordinates);
        var projectComp = EnsureComp<CEUnselectedDiscoveryProjectComponent>(project);
        projectComp.Player = args.Actor;
        projectComp.Candidates = candidates;
        Dirty(project, projectComp);

        _container.Remove(paper, container);
        Del(paper);

        var authorName = MetaData(args.Actor).EntityName;
        var authorLine = Loc.GetString("ce-knowledge-book-author", ("name", authorName));
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
            !_proto.TryIndex(discovery.Knowledge, out var knowledge) ||
            !_proto.TryIndex(discovery.Area, out var area))
            return;

        if (!_science.TryGetSingleton(out _))
            return;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.PaperSlotId, out var container))
            return;

        _container.Remove(item, container);
        Del(item);

        var project = Spawn(area.Project, Transform(ent.Owner).Coordinates);
        var projectComp = EnsureComp<CEDiscoveryProjectComponent>(project);
        projectComp.Discovery = args.Discovery;
        projectComp.Tiles = _science.GenerateMap(discovery);
        Dirty(project, projectComp);

        var discoveryName = Loc.GetString(knowledge.Name);
        _metaData.SetEntityName(project, Loc.GetString("ce-discovery-project-name", ("discovery", discoveryName)));

        var description = Loc.GetString("ce-discovery-project-description",
            ("area", Loc.GetString(area.Name)),
            ("discovery", discoveryName));
        var authorLine = Loc.GetString("ce-knowledge-book-author", ("name", MetaData(args.Actor).EntityName));
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
            !_proto.TryIndex(discovery.Knowledge, out _))
            return;

        if (!CanPlaceAspect(project, discovery.Generation.Radius, args.Hex, args.Essence))
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

        if (!_proto.TryIndex(project.Discovery, out var discovery) ||
            !_proto.TryIndex(discovery.Knowledge, out var knowledge))
            return;

        if (!IsProjectSolved(project.Tiles))
            return;

        if (!_science.TryGetSingleton(out var science))
            return;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.PaperSlotId, out var container))
            return;

        _knowledge.TryLearn(args.Actor, discovery.Knowledge);
        _science.MarkChosen(science, project.Discovery);

        _container.Remove(item, container);
        Del(item);

        var spawned = Spawn(knowledge.Book, Transform(ent.Owner).Coordinates);

        // Knowledge must be set before the component is added, not after via EnsureComp - it's
        // read by the component's own MapInit handler, which AddComp fires immediately since the
        // entity is already map-initialized (that handler also takes care of the base name/description).
        AddComp(spawned, new CEKnowledgeHolderComponent { Knowledge = discovery.Knowledge });

        var descLines = new List<string>();

        var effects = _knowledge.GetKnowledgeEffectDescription(discovery.Knowledge);
        if (effects.Length > 0)
            descLines.Add(effects);

        var authorName = MetaData(args.Actor).EntityName;
        descLines.Add(Loc.GetString("ce-knowledge-book-author", ("name", authorName)));
        _metaData.SetEntityDescription(spawned, string.Join("\n", descLines));

        _ui.CloseUi(ent.Owner, CEResearchTableUiKey.Key);

        _audio.PlayPvs(KnowledgeLearnedSound, ent.Owner);
    }

    private bool CanPlaceAspect(
        CEDiscoveryProjectComponent project,
        int radius,
        Vector2i hex,
        ProtoId<CEMagicEssenceTypePrototype> essence)
    {
        if (CEHexMath.CubeDistance(hex, Vector2i.Zero) > radius)
            return false;

        if (project.Tiles.TryGetValue(hex, out var existing) && (existing.DeadZone || existing.Aspect is not null))
            return false;

        foreach (var neighbor in CEHexMath.Neighbors(hex))
        {
            if (project.Tiles.TryGetValue(neighbor, out var tile) &&
                tile.Aspect is { } neighborAspect &&
                _essence.AreDirectlyRelated(neighborAspect, essence))
                return true;
        }

        return false;
    }
}
