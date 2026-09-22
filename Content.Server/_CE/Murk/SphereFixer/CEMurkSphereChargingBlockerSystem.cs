using System.Numerics;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Murk.Systems;

namespace Content.Server._CE.Murk.SphereFixer;

public sealed partial class CEMurkSphereChargingBlockerSystem : EntitySystem
{
    [Dependency] private CESharedMurkSystem _murk = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CEMurkSphereFixerSystem _sphereFixer = default!;

    private const float RecheckInterval = 5f;
    private float _recheckTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMurkSphereFixerBlockRefreshEvent>(OnBlockRefresh);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _recheckTimer += frameTime;
        if (_recheckTimer < RecheckInterval)
            return;

        _recheckTimer -= RecheckInterval;

        // Blockers (e.g. a roaming Lurker) can move relative to the sphere without anything else
        // on the fixer changing, so their position needs periodic rechecking.
        if (EntityQueryEnumerator<CEMurkLusconSphereComponent>().MoveNext(out _))
            _sphereFixer.RefreshBlockConditions();
    }

    [SubscribeLocalEvent]
    private void OnBlockerStartup(Entity<CEMurkSphereChargingBlockerComponent> ent, ref ComponentStartup args)
    {
        _sphereFixer.RefreshBlockConditions();
    }

    [SubscribeLocalEvent]
    private void OnBlockerShutdown(Entity<CEMurkSphereChargingBlockerComponent> ent, ref ComponentShutdown args)
    {
        _sphereFixer.RefreshBlockConditions();
    }

    private void OnBlockRefresh(CEMurkSphereFixerBlockRefreshEvent args)
    {
        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent, CEMurkSourceComponent, TransformComponent>();
        while (sphereQuery.MoveNext(out var sphereUid, out _, out var source, out var sphereXform))
        {
            if (!source.Active)
                continue;

            var sphereWorldPos = _transform.GetWorldPosition(sphereUid);

            var blockerQuery = EntityQueryEnumerator<CEMurkSphereChargingBlockerComponent, TransformComponent>();
            while (blockerQuery.MoveNext(out var blockerUid, out var blocker, out var blockerXform))
            {
                if (blockerXform.MapUid is not { } blockerMap)
                    continue;

                if (!_murk.TryProjectSource(blockerMap, (sphereUid, sphereXform), source.Intensity, out var radius))
                    continue;

                var distance = Vector2.Distance(sphereWorldPos, _transform.GetWorldPosition(blockerUid));
                if (distance >= radius)
                    continue;

                args.Block(Loc.GetString(blocker.Name),
                    Loc.GetString("ce-murk-sphere-charging-blocker-desc"),
                    blockerXform.Coordinates);
            }
        }
    }
}
