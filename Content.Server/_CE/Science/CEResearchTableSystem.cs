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
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private readonly EntProtoId _projectProto = "CEUnselectedDiscoveryProject";

    private static readonly SoundSpecifier StartResearchSound = new SoundCollectionSpecifier("PaperScribbles");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableMergeAspectsMessage>(OnMergeAspects);
        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableStartResearchMessage>(OnStartResearch);
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
        var projectComp = Comp<CEUnselectedDiscoveryProjectComponent>(project);
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

        _audio.PlayPvs(StartResearchSound, ent.Owner);
    }
}
