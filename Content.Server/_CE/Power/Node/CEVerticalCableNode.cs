using Content.Server._CE.ZLevels.Core;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.Nodes;

[DataDefinition]
public sealed partial class CECableVerticalNode : Node
{
    [DataField]
    public bool Up;

    [DataField]
    public bool Down;

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

        List<Node> outputNodes = new();


        foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, grid, gridIndex))
        {
            if (node is CableNode)
                outputNodes.Add(node);
        }

        var zLevelsSys = entMan.System<CEZLevelsSystem>();

        if (Up && zLevelsSys.TryMapUp(xform.MapUid.Value, out var mapAbove) && entMan.TryGetComponent<MapGridComponent>(mapAbove, out var mapAboveGrid))
        {
            var nodesAbove = NodeHelpers.GetNodesInTile(nodeQuery, mapAboveGrid, gridIndex);

            foreach (var nodeAbove in nodesAbove)
            {
                if (nodeAbove is CECableVerticalNode verticalCableNode && verticalCableNode.Down)
                    outputNodes.Add(nodeAbove);
            }
        }

        if (Down && zLevelsSys.TryMapDown(xform.MapUid.Value, out var mapDown) && entMan.TryGetComponent<MapGridComponent>(mapDown, out var mapDownGrid))
        {
            var nodesDown = NodeHelpers.GetNodesInTile(nodeQuery, mapDownGrid, gridIndex);

            foreach (var nodeDown in nodesDown)
            {
                if (nodeDown is CECableVerticalNode verticalCableNode && verticalCableNode.Up)
                    outputNodes.Add(nodeDown);
            }
        }

        foreach (var node in outputNodes)
        {
            yield return node;
        }
    }
}
