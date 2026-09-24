using Content.Server._CE.MurkSphere.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Murk;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Power;
using Robust.Shared.Map;

namespace Content.Server._CE.MurkSphere;

/// <summary>
/// Each pylon checks its own conditions independently - powered, in the murk, and far enough from
/// every other powered pylon (see <see cref="CEMurkSphereFixerComponent.PylonsMinRadius"/>). Extra
/// broken pylons beyond <see cref="CEMurkSphereFixerComponent.PylonsRequired"/> don't matter as
/// long as enough others pass.
/// </summary>
public sealed partial class CEMurkPylonSystem : EntitySystem
{
    [Dependency] private CEMurkSphereFixerSystem _sphereFixer = default!;
    [Dependency] private CESharedMurkSystem _murk = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMurkSphereFixerBlockRefreshEvent>(OnBlockRefresh);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEMurkPylonComponent> ent, ref MapInitEvent args)
    {
        _sphereFixer.RefreshBlockConditions();
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CEMurkPylonComponent> ent, ref PowerChangedEvent args)
    {
        _sphereFixer.RefreshBlockConditions();
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CEMurkPylonComponent> ent, ref ComponentShutdown args)
    {
        _sphereFixer.RefreshBlockConditions();
    }

    private void OnBlockRefresh(CEMurkSphereFixerBlockRefreshEvent args)
    {
        if (!EntityQueryEnumerator<CEMurkSphereFixerComponent>().MoveNext(out var fixer))
            return;

        if (!EntityQueryEnumerator<CEMurkLusconSphereComponent, TransformComponent>().MoveNext(out _, out var sphereXform))
            return;

        if (sphereXform.MapUid is not { } sphereMap)
            return;

        // Pylons that aren't even on a z-level connected to the sphere (and by extension the
        // monolith) are irrelevant - not counted, and not compared against as neighbors either.
        var pylons = new List<(EntityUid Uid, TransformComponent Xform)>();
        var pylonQuery = EntityQueryEnumerator<CEMurkPylonComponent, TransformComponent>();
        while (pylonQuery.MoveNext(out var pylonUid, out _, out var pylonXform))
        {
            if (pylonXform.MapUid is not { } pylonMap || !_zLevels.TryGetZLevelOffset(sphereMap, pylonMap, out _))
                continue;

            pylons.Add((pylonUid, pylonXform));
        }

        var validCount = 0;
        var problemPylons = new List<EntityCoordinates>();

        foreach (var (pylonUid, pylonXform) in pylons)
        {
            var powered = _power.IsPowered(pylonUid);
            var inMurk = _murk.InMurk(pylonUid, pylonXform);
            var tooClose = false;

            if (powered)
            {
                foreach (var (otherUid, _) in pylons)
                {
                    if (otherUid == pylonUid || !_power.IsPowered(otherUid))
                        continue;

                    if (!_zLevels.TryGetEffectiveDistance(pylonUid, otherUid, out var distance))
                        continue;

                    if (distance < fixer.PylonsMinRadius)
                    {
                        tooClose = true;
                        break;
                    }
                }
            }

            if (powered && inMurk && !tooClose)
            {
                validCount++;
                continue;
            }

            problemPylons.Add(pylonXform.Coordinates);

            if (!powered)
            {
                args.Block(Loc.GetString("ce-murk-pylon-block-unpowered-title"),
                    Loc.GetString("ce-murk-pylon-block-unpowered-desc"),
                    pylonXform.Coordinates);
            }
            else if (!inMurk)
            {
                args.Block(Loc.GetString("ce-murk-pylon-block-outside-murk-title"),
                    Loc.GetString("ce-murk-pylon-block-outside-murk-desc"),
                    pylonXform.Coordinates);
            }
            else
            {
                args.Block(Loc.GetString("ce-murk-pylon-block-too-close-title"),
                    Loc.GetString("ce-murk-pylon-block-too-close-desc"),
                    pylonXform.Coordinates);
            }
        }

        if (validCount < fixer.PylonsRequired)
        {
            args.Block(Loc.GetString("ce-murk-pylon-block-count-title"),
                Loc.GetString("ce-murk-pylon-block-count-desc", ("count", validCount), ("required", fixer.PylonsRequired)),
                problemPylons);
        }
    }
}
