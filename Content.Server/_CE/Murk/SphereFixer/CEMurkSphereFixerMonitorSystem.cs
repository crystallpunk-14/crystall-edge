using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Murk.SphereFixer;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._CE.Murk.SphereFixer;

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
        var blocked = false;
        var fixerQuery = EntityQueryEnumerator<CEMurkSphereFixerComponent>();
        while (fixerQuery.MoveNext(out var fixer))
        {
            if (fixer.Blocked)
                blocked = true;
        }

        _appearance.SetData(uid, CEMurkSphereFixerMonitorVisuals.Blocked, blocked);
    }

    private void PushState(EntityUid uid)
    {
        if (!_ui.IsUiOpen(uid, CEMurkSphereFixerMonitorUiKey.Key))
            return;

        var charge = 0f;
        var fixerExists = false;
        var blockers = new List<CEMurkSphereFixerBlockerInfo>();
        var fixerQuery = EntityQueryEnumerator<CEMurkSphereFixerComponent>();
        while (fixerQuery.MoveNext(out var fixer))
        {
            charge = MathF.Max(fixer.Charge, charge);
            fixerExists = true;

            foreach (var blocker in fixer.Blockers)
            {
                var coordinates = new List<NetCoordinates>(blocker.Coordinates.Count);
                foreach (var coords in blocker.Coordinates)
                {
                    coordinates.Add(GetNetCoordinates(coords));
                }

                blockers.Add(new CEMurkSphereFixerBlockerInfo(blocker.Title, blocker.Description, coordinates));
            }
        }

        NetCoordinates? sphereCoordinates = null;
        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent, TransformComponent>();
        if (sphereQuery.MoveNext(out _, out var sphereXform))
            sphereCoordinates = GetNetCoordinates(sphereXform.Coordinates);

        if (!fixerExists)
        {
            blockers.Add(new CEMurkSphereFixerBlockerInfo(
                Loc.GetString("ce-murk-sphere-fixer-block-missing-title"),
                Loc.GetString("ce-murk-sphere-fixer-block-missing-desc"),
                new List<NetCoordinates>()));
        }

        _ui.SetUiState(uid,
            CEMurkSphereFixerMonitorUiKey.Key,
            new CEMurkSphereFixerMonitorBoundUserInterfaceState(charge, sphereCoordinates, blockers));
    }
}
