using Content.Server._CE.Science.Components;
using Content.Shared._CE.EntityEffect;
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
        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableActionMessage>(OnAction);
    }

    private void OnBeforeUIOpen(Entity<CEResearchTableComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        SendState(ent, args.User);
    }

    private void OnAction(Entity<CEResearchTableComponent> ent, ref CEResearchTableActionMessage args)
    {
        if (!_proto.TryIndex(args.Action, out var action))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(args.Actor);

        // The coordinate must already be researched by this player - actions only ever operate on
        // cells the player can already see, never on unrevealed ones.
        if (!data.Researched.TryGetValue(args.Area, out var researched) || !researched.Contains(args.Coordinate))
            return;

        var kind = CEResearchCellKind.Empty;
        if (_science.TryGetSingleton(out var science)
            && science.Areas.TryGetValue(args.Area, out var areaCells)
            && areaCells.TryGetValue(args.Coordinate, out var cell))
        {
            kind = cell.Kind;
        }

        if (!action.AllowedCells.HasFlag(kind))
            return;

        var conditionArgs = new CEEntityEffectArgs(EntityManager, args.Actor, null, default, 0f, args.Actor, null);
        foreach (var condition in action.Conditions)
        {
            if (!condition.Passes(conditionArgs))
                return;
        }

        var effectArgs = new CEResearchActionEffectArgs(EntityManager, args.Actor, args.Area, args.Coordinate);
        foreach (var effect in action.Effects)
        {
            effect.Effect(effectArgs);
        }

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
