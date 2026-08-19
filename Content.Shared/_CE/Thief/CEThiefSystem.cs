using Content.Shared._CE.Waypointer;
using Robust.Shared.Player;

namespace Content.Shared._CE.Thief;

/// <summary>
/// Handles toggling the thief's treasure sense ability.
/// </summary>
public sealed partial class CEThiefSystem : EntitySystem
{
    [Dependency] private CESharedWaypointerSystem _waypointer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, CEThiefToggleTreasureSenseEvent>(OnToggleTreasureSense);
        SubscribeLocalEvent<CETreasureSenseComponent, CERefreshWaypointersEvent>(OnRefreshWaypointers);
    }

    private void OnToggleTreasureSense(Entity<ActorComponent> ent, ref CEThiefToggleTreasureSenseEvent args)
    {
        if (args.Handled)
            return;

        // Refresh is called explicitly here rather than from ComponentInit/ComponentShutdown:
        // during RemComp's own ComponentShutdown, the component being removed is still visible
        // to HasComp/subscriptions, so a refresh triggered from there would still pick it up.
        if (HasComp<CETreasureSenseComponent>(ent))
            RemComp<CETreasureSenseComponent>(ent);
        else
            AddComp<CETreasureSenseComponent>(ent);

        _waypointer.RefreshWaypointers(ent);

        // Without this in Shared, the action doesn't toggle.
        args.Toggle = true;
        args.Handled = true;
    }

    private void OnRefreshWaypointers(Entity<CETreasureSenseComponent> ent, ref CERefreshWaypointersEvent args)
    {
        args.WaypointerProtoIds.UnionWith(ent.Comp.WaypointerProtoIds);
    }
}
