using Content.Server._CE.Power.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.Power;

/// <summary>
/// Forces neighboring <see cref="NodeContainerComponent"/> nodes to recompute their connections
/// whenever a <see cref="CEIsolatorComponent"/> entity anchors or unanchors nearby. Necessary
/// because reflooding otherwise only cascades to a neighbor if the isolator's own node connects to
/// it (matching type and NodeGroupID) — which doesn't happen for e.g. valves, which expose no
/// <c>CableNode</c> for the isolator to touch.
/// </summary>
public sealed partial class CEIsolatorSystem : EntitySystem
{
    [Dependency] private NodeGroupSystem _nodeGroup = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEIsolatorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CEIsolatorComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnStartup(Entity<CEIsolatorComponent> ent, ref ComponentStartup args)
    {
        RefloodNeighbors(ent);
    }

    private void OnAnchorStateChanged(Entity<CEIsolatorComponent> ent, ref AnchorStateChangedEvent args)
    {
        RefloodNeighbors(ent);
    }

    private void RefloodNeighbors(EntityUid uid)
    {
        var xform = Transform(uid);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var gridEnt = new Entity<MapGridComponent>(gridUid, grid);
        var gridIndex = _mapSystem.TileIndicesFor(gridEnt, xform.Coordinates);
        var nodeQuery = GetEntityQuery<NodeContainerComponent>();

        foreach (var (_, node) in NodeHelpers.GetCardinalNeighborNodes(nodeQuery, gridEnt, gridIndex, _mapSystem))
        {
            _nodeGroup.QueueReflood(node);
        }
    }
}
