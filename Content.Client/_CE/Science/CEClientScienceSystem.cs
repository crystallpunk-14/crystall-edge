using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._CE.Science;

public sealed partial class CEClientScienceSystem : CESharedScienceSystem
{
    [Dependency] private IPlayerManager _player = default!;

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
}
