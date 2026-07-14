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

        List<Node> outputNodes = new();


        foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, gridEnt, gridIndex, mapSystem))
        {
            if (node is CableNode)
                outputNodes.Add(node);
        }

        var zLevelsSys = entMan.System<CEZLevelsSystem>();

        if (Up && zLevelsSys.TryMapUp(xform.Comp.MapUid.Value, out var mapAbove) && entMan.TryGetComponent<MapGridComponent>(mapAbove.Value.Owner, out var mapAboveGrid))
        {
            var nodesAbove = NodeHelpers.GetNodesInTile(nodeQuery, (mapAbove.Value.Owner, mapAboveGrid), gridIndex, mapSystem);

            foreach (var nodeAbove in nodesAbove)
            {
                if (nodeAbove is CECableVerticalNode verticalCableNode && verticalCableNode.Down)
                    outputNodes.Add(nodeAbove);
            }
        }

        if (Down && zLevelsSys.TryMapDown(xform.Comp.MapUid.Value, out var mapDown) && entMan.TryGetComponent<MapGridComponent>(mapDown.Value.Owner, out var mapDownGrid))
        {
            var nodesDown = NodeHelpers.GetNodesInTile(nodeQuery, (mapDown.Value.Owner, mapDownGrid), gridIndex, mapSystem);

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
