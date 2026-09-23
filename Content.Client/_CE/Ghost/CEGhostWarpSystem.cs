using Content.Shared._CE.Ghost;
using Content.Shared.Ghost.Components;
using Robust.Client.Player;

namespace Content.Client._CE.Ghost;

/// <summary>
/// CE-owned counterpart to Content.Client.Ghost.GhostSystem's warp request/response handling,
/// kept separate so the warp target list (<see cref="CEGhostWarp"/>) can grow CE-specific fields
/// without editing the upstream system.
/// </summary>
public sealed partial class CEGhostWarpSystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerManager = default!;

    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;

    public event Action<CEGhostWarpsResponseEvent>? CEGhostWarpsResponse;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CEGhostWarpsResponseEvent>(OnCEGhostWarpsResponse);
    }

    private void OnCEGhostWarpsResponse(CEGhostWarpsResponseEvent msg)
    {
        if (_playerManager.LocalEntity is not { } local || !_ghostQuery.HasComp(local))
            return;

        CEGhostWarpsResponse?.Invoke(msg);
    }

    public void RequestWarps()
    {
        RaiseNetworkEvent(new CEGhostWarpsRequestEvent());
    }
}
