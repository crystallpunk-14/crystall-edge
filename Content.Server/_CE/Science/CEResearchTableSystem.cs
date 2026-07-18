using Content.Server._CE.Science.Components;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Prototypes;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science;

public sealed class CEResearchTableSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private CEScienceSystem _science = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEResearchTableComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableResearchMessage>(OnResearch);
    }

    private void OnBeforeUIOpen(Entity<CEResearchTableComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        SendState(ent, args.User);
    }

    private void OnResearch(Entity<CEResearchTableComponent> ent, ref CEResearchTableResearchMessage args)
    {
        var data = EnsureComp<CEScienceResearchDataComponent>(args.Actor);

        if (!data.Researched.TryGetValue(args.Area, out var researched))
        {
            researched = new HashSet<Vector2i>();
            data.Researched[args.Area] = researched;
        }

        researched.Add(args.Coordinate);

        SendState(ent, args.Actor);
    }

    private void SendState(EntityUid uid, EntityUid actor)
    {
        if (!_science.TryGetSingleton(out var science))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(actor);

        var areas = new Dictionary<ProtoId<CEScienceAreaPrototype>, CEResearchTableAreaData>();
        foreach (var area in _proto.EnumeratePrototypes<CEScienceAreaPrototype>())
        {
            var researched = data.Researched.TryGetValue(area.ID, out var set) ? set : new HashSet<Vector2i>();
            var cells = new Dictionary<Vector2i, CEScienceMapCell>();

            if (science.Areas.TryGetValue(area.ID, out var areaCells))
            {
                foreach (var coordinate in researched)
                {
                    if (areaCells.TryGetValue(coordinate, out var cell))
                        cells[coordinate] = cell;
                }
            }

            areas[area.ID] = new CEResearchTableAreaData(cells, researched);
        }

        _userInterface.SetUiState(uid, CEResearchTableUiKey.Key, new CEResearchTableState(areas));
    }
}
