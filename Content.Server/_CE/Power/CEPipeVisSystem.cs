using Content.Server._CE.Power.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.NodeContainer;
using Content.Shared.Wires;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.Power;

[UsedImplicitly]
public sealed class CEPipeVisSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<NodeContainerComponent> _nodeQuery;

    public override void Initialize()
    {
        base.Initialize();

        _transformQuery = GetEntityQuery<TransformComponent>();
        _nodeQuery = GetEntityQuery<NodeContainerComponent>();

        SubscribeLocalEvent<CEPipeVisComponent, NodeGroupsRebuilt>(UpdateAppearance);
    }

    private void UpdateAppearance(EntityUid uid, CEPipeVisComponent cableVis, ref NodeGroupsRebuilt args)
    {
        if (!TryComp<NodeContainerComponent>(uid, out var nodeContainer))
            return;

        var transform = Transform(uid);
        if (!TryComp<MapGridComponent>(transform.GridUid, out var grid))
            return;

        var mask = WireVisDirFlags.None;
        var tile = _map.TileIndicesFor((transform.GridUid.Value, grid), transform.Coordinates);

        foreach (var (name, node) in nodeContainer.Nodes)
        {
            if (node is not CableNode && node is not CEConnectorEdgeNode)
                continue;


            foreach (var reachable in
                     node.GetReachableNodes(transform,
                         _nodeQuery,
                         _transformQuery,
                         grid,
                         EntityManager))
            {
                if (reachable is not CableNode && reachable is not CEConnectorEdgeNode)
                    continue;

                if (reachable.NodeGroupID != node.NodeGroupID)
                    continue;

                var otherTransform = Transform(reachable.Owner);
                var otherTile = _map.TileIndicesFor((transform.GridUid.Value, grid), otherTransform.Coordinates);
                var diff = otherTile - tile;

                mask |= diff switch
                {
                    (0, 1) => WireVisDirFlags.North,
                    (0, -1) => WireVisDirFlags.South,
                    (1, 0) => WireVisDirFlags.East,
                    (-1, 0) => WireVisDirFlags.West,
                    _ => WireVisDirFlags.None
                };
            }
        }

        _appearance.SetData(uid, WireVisVisuals.ConnectedMask, mask);
    }
}
