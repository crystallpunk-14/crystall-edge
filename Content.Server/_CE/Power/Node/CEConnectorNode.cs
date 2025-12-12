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

    public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        if (!xform.Anchored || grid == null)
            yield break;

        var gridIndex = grid.TileIndicesFor(xform.Coordinates);

        if (xform.MapUid is null)
            yield break;

        List<(Direction, Node)> nodeDirs = new();

        foreach (var (dir, node) in NodeHelpers.GetCardinalNeighborNodes(nodeQuery, grid, gridIndex))
        {
            if (node is CableNode && Direction == dir)
            {
                nodeDirs.Add((dir, node));
            }

            if (node is CEConnectorCenterNode center && center.Active)
                nodeDirs.Add((dir, node));
        }

        foreach (var (dir, node) in nodeDirs)
        {
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

    public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        if (!xform.Anchored || grid == null || !Active)
            yield break;

        var gridIndex = grid.TileIndicesFor(xform.Coordinates);

        if (xform.MapUid is null)
            yield break;

        List<Node> connectNodes = new();

        foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, grid, gridIndex))
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
