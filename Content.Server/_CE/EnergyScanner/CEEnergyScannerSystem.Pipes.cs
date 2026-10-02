using Content.Server._CE.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Nodes;
using Content.Shared._CE.EnergyScanner;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.Timing;

namespace Content.Server._CE.EnergyScanner;

public sealed partial class CEEnergyScannerSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan PipeUpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Throttle timer only - not game state, it is fine to lose it on save / load.
    /// </summary>
    private TimeSpan _nextPipeUpdate;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextPipeUpdate)
            return;

        _nextPipeUpdate = _timing.CurTime + PipeUpdateInterval;

        // SetData skips unchanged values, so only actual flips get networked.
        var query = EntityQueryEnumerator<CEPipeVisComponent, NodeContainerComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var nodeContainer, out var appearance))
        {
            var powered = false;
            var large = false;
            var vertical = CEPipeVerticalDirection.None;

            foreach (var node in nodeContainer.Nodes.Values)
            {
                if (node.NodeGroupID == NodeGroupID.HVPower)
                    large = true;

                if (node is CECableVerticalNode verticalNode)
                {
                    if (verticalNode.Up)
                        vertical |= CEPipeVerticalDirection.Up;
                    if (verticalNode.Down)
                        vertical |= CEPipeVerticalDirection.Down;
                }

                if (node.NodeGroup is IBasePowerNet net && IsPowered(net))
                    powered = true;
            }

            _appearance.SetData(uid, CEEnergyScannerVisuals.Powered, powered, appearance);
            _appearance.SetData(uid, CEEnergyScannerVisuals.Large, large, appearance);
            _appearance.SetData(uid, CEEnergyScannerVisuals.Vertical, vertical, appearance);
        }
    }
}
