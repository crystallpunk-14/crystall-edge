using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Nodes;

/// <summary>
/// A node that connects to cables or active center nodes in a specific direction on the grid.
/// </summary>
[DataDefinition]
public sealed partial class CEConnectorEdgeNode : Node
{
    [DataField(required: true)]
    public Direction Direction = Direction.Invalid;

    public override IEnumerable<Node> GetReachableNodes(Entity<TransformComponent> xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        Entity<MapGridComponent>? grid,
        IEntityManager entMan)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt)
            yield break;

        var mapSystem = entMan.System<SharedMapSystem>();
        var gridIndex = mapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        if (xform.Comp.MapUid is null)
            yield break;

        // If a cable isolator is facing this direction (on our tile) or facing back at us
        // (on the neighbor tile), the connection across that direction is severed.
        var blocked = false;
        List<(Direction, Node)> nodeDirs = new();

        foreach (var (dir, node) in NodeHelpers.GetCardinalNeighborNodes(nodeQuery, gridEnt, gridIndex, mapSystem))
        {
            if (node is CableNode && Direction == dir)
            {
                nodeDirs.Add((dir, node));
            }

            if (node is CEConnectorCenterNode center && center.Active)
                nodeDirs.Add((dir, node));

            if (node is CableTerminalNode)
            {
                var terminalDir = xformQuery.GetComponent(node.Owner).LocalRotation.GetCardinalDir();
                if (dir == Direction.Invalid && terminalDir == Direction)
                    blocked = true;
                else if (dir == Direction && terminalDir.GetOpposite() == dir)
                    blocked = true;
            }
        }

        foreach (var (dir, node) in nodeDirs)
        {
            // Own-tile center link (dir == Invalid) is never blocked, only the cross-tile ones.
            if (blocked && dir == Direction && (node is CableNode || node is CEConnectorCenterNode))
                continue;

            yield return node;
        }
    }
}

/// <summary>
/// A central connector node that can be toggled to enable or disable connections to edge nodes.
/// </summary>
[DataDefinition]
public sealed partial class CEConnectorCenterNode : Node
{
    /// <summary>
    /// If disabled, this cable will never connect.
    /// </summary>
    /// <remarks>
    /// If you change this,
    /// you must manually call <see cref="NodeGroupSystem.QueueReflood"/> to update the node connections.
    /// </remarks>
    [DataField]
    public bool Active = true;

    public override bool Connectable(IEntityManager entMan, TransformComponent? xform = null)
    {
        if (!Active)
            return false;

        return base.Connectable(entMan, xform);
    }

    public override IEnumerable<Node> GetReachableNodes(Entity<TransformComponent> xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        Entity<MapGridComponent>? grid,
        IEntityManager entMan)
    {
        if (!xform.Comp.Anchored || grid is not { } gridEnt || !Active)
            yield break;

        var mapSystem = entMan.System<SharedMapSystem>();
        var gridIndex = mapSystem.TileIndicesFor(gridEnt, xform.Comp.Coordinates);

        if (xform.Comp.MapUid is null)
            yield break;

        List<Node> connectNodes = new();

        foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, gridEnt, gridIndex, mapSystem))
        {
            if (node is CEConnectorEdgeNode)
                connectNodes.Add(node);
        }

        foreach (var node in connectNodes)
        {
            yield return node;
        }
    }
}
