using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._CE.Science;

public sealed partial class CEClientScienceSystem : CESharedScienceSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly EntProtoId _interestVfx = "CEScientificInterestVFX";

    private TimeSpan _nextInterestReveal = TimeSpan.Zero;

    /// <summary>
    /// Raised whenever the local player's own <see cref="CEScienceResearchDataComponent"/> state is
    /// updated from the server. The research table's <see cref="CEResearchTableState"/> BUI push and
    /// this component's own networked state are two independent sync channels that can arrive in
    /// either order within the same tick - UI that reads this component locally (fog of war, points)
    /// should refresh on both, not just the BUI push, or it can render one tick stale.
    /// </summary>
    public event Action? OnLocalResearchDataUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEScienceResearchDataComponent, AfterAutoHandleStateEvent>(OnResearchDataState);
    }

    private void OnResearchDataState(Entity<CEScienceResearchDataComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner == _player.LocalEntity)
            OnLocalResearchDataUpdated?.Invoke();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextInterestReveal)
            return;

        if (_player.LocalEntity is not { } player || !TryGetHeldMagnifyingGlass(player, out var interval))
            return;

        _nextInterestReveal = _timing.CurTime + interval;

        var query = EntityQueryEnumerator<CEScientificInterestComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var interest, out var xform))
        {
            if (interest.StudiedBy.Contains(player))
                continue;

            var vfx = SpawnAtPosition(_interestVfx, xform.Coordinates);
            _transform.SetParent(vfx, uid);
        }
    }

    private bool TryGetHeldMagnifyingGlass(EntityUid player, out TimeSpan interval)
    {
        interval = default;

        if (!_hands.TryGetActiveItem(player, out var held) ||
            !TryComp<CEThaumaturgicMagnifyingGlassComponent>(held, out var glass))
            return false;

        interval = glass.RevealInterval;
        return true;
    }
}
