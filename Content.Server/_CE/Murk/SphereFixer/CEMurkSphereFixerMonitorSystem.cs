using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Murk.SphereFixer;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._CE.Murk.SphereFixer;

/// <summary>
/// Pushes the Pillar of Light's charge and current blockers to any open monitor console, and
/// keeps its sprite showing whether the Pillar has blockers (layer visibility itself is handled
/// by the engine's own PowerDeviceVisuals.Powered). The console pushes state rather than the
/// client reading the Pillar's component directly, since a monitor can be placed far from the
/// (server-only) Pillar entity.
/// </summary>
public sealed partial class CEMurkSphereFixerMonitorSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private const float UpdateInterval = 1f;
    private float _updateTimer;

    [SubscribeLocalEvent]
    private void OnBoundUIOpened(Entity<CEMurkSphereFixerMonitorComponent> ent, ref BoundUIOpenedEvent args)
    {
        PushState(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer < UpdateInterval)
            return;

        _updateTimer -= UpdateInterval;

        var query = EntityQueryEnumerator<CEMurkSphereFixerMonitorComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            UpdateVisuals(uid);
            PushState(uid);
        }
    }

    private void UpdateVisuals(EntityUid uid)
    {
        var fixerQuery = EntityQueryEnumerator<CEMurkSphereFixerComponent>();
        fixerQuery.MoveNext(out var fixer);

        _appearance.SetData(uid, CEMurkSphereFixerMonitorVisuals.Blocked, fixer?.Blocked ?? true);
    }

    private void PushState(EntityUid uid)
    {
        if (!_ui.IsUiOpen(uid, CEMurkSphereFixerMonitorUiKey.Key))
            return;

        var fixerQuery = EntityQueryEnumerator<CEMurkSphereFixerComponent>();
        fixerQuery.MoveNext(out var fixer);

        NetCoordinates? sphereCoordinates = null;
        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent, TransformComponent>();
        if (sphereQuery.MoveNext(out _, out var sphereXform))
            sphereCoordinates = GetNetCoordinates(sphereXform.Coordinates);

        var blockers = new List<CEMurkSphereFixerBlockerInfo>();
        if (fixer != null)
        {
            foreach (var blocker in fixer.Blockers)
                blockers.Add(new CEMurkSphereFixerBlockerInfo(blocker.Title, blocker.Description, GetNetCoordinates(blocker.Coordinates)));
        }
        else
        {
            blockers.Add(new CEMurkSphereFixerBlockerInfo(
                Loc.GetString("ce-murk-sphere-fixer-block-missing-title"),
                Loc.GetString("ce-murk-sphere-fixer-block-missing-desc"),
                null));
        }

        _ui.SetUiState(uid,
            CEMurkSphereFixerMonitorUiKey.Key,
            new CEMurkSphereFixerMonitorBoundUserInterfaceState(fixer?.Charge ?? 0f, sphereCoordinates, blockers));
    }
}
