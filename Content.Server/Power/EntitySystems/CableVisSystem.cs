using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.NodeContainer;
using Content.Shared.Wires;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    public sealed class CableVisSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;

        private EntityQuery<TransformComponent> _transformQuery; //CrystallEdge
        private EntityQuery<NodeContainerComponent> _nodeQuery; //CrystallEdge

        public override void Initialize()
        {
            base.Initialize();

            //CrystallEdge
            _transformQuery = GetEntityQuery<TransformComponent>();
            _nodeQuery = GetEntityQuery<NodeContainerComponent>();
            //CrystallEdge end

            SubscribeLocalEvent<CableVisComponent, NodeGroupsRebuilt>(UpdateAppearance);
        }

        private void UpdateAppearance(EntityUid uid, CableVisComponent cableVis, ref NodeGroupsRebuilt args)
        {
            if (!_nodeContainer.TryGetNode(uid, cableVis.Node, out CableNode? node))
                return;

            var transform = Transform(uid);
            if (!TryComp<MapGridComponent>(transform.GridUid, out var grid))
                return;

            var mask = WireVisDirFlags.None;
            var tile = _map.TileIndicesFor((transform.GridUid.Value, grid), transform.Coordinates);

            foreach (var reachable in node.GetReachableNodes(transform, _nodeQuery, _transformQuery, grid, EntityManager)) //CrystallEdge connect also disabled nodes, for pipe levers support
            {
                if (reachable is not CableNode)
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

            _appearance.SetData(uid, WireVisVisuals.ConnectedMask, mask);
        }
    }
}
