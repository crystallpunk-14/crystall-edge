using Content.Server._CE.Science.Components;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Prototypes;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Science;

public sealed partial class CEResearchTableSystem : EntitySystem
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

        if (!_science.HasEnoughPoints((args.Actor, data), action.Cost))
            return;

        var conditionArgs = new CEEntityEffectArgs(EntityManager, args.Actor, null, default, 0f, args.Actor, null);
        foreach (var condition in action.Conditions)
        {
            if (!condition.Passes(conditionArgs))
                return;
        }

        _science.TrySpendPoints((args.Actor, data), action.Cost);

        var effectArgs = new CEResearchActionEffectArgs(EntityManager, ent, args.Actor, args.Area, args.Coordinate);
        foreach (var effect in action.Effects)
        {
            effect.Effect(effectArgs);
        }

        SendState(ent, args.Actor);
    }

    /// <summary>
    /// Pushes each area's cell content, filtered down to only what <paramref name="actor"/> has
    /// personally researched - that Researched set is data the actor's own client already knows
    /// (it lives on their own networked <see cref="CEScienceResearchDataComponent"/>), so filtering
    /// by it here doesn't hand out anything new. The raw Researched set and the actor's points are
    /// never put in this (shared, per-table) state - those are read by the client locally off its
    /// own component instead.
    /// </summary>
    private void SendState(EntityUid uid, EntityUid actor)
    {
        if (!_science.TryGetSingleton(out var science))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(actor);

        var areas = new Dictionary<ProtoId<CEScienceAreaPrototype>, CEResearchTableAreaData>();
        foreach (var area in _proto.EnumeratePrototypes<CEScienceAreaPrototype>())
        {
            var cells = new Dictionary<Vector2i, CEScienceMapCell>();

            if (data.Researched.TryGetValue(area.ID, out var researched)
                && science.Areas.TryGetValue(area.ID, out var areaCells))
            {
                foreach (var coordinate in researched)
                {
                    if (areaCells.TryGetValue(coordinate, out var cell))
                        cells[coordinate] = cell;
                }
            }

            areas[area.ID] = new CEResearchTableAreaData(cells);
        }

        _userInterface.SetUiState(uid, CEResearchTableUiKey.Key, new CEResearchTableState(areas));
    }
}
