using System.Numerics;
using Content.Server._CE.ZLevels.Core;
using Content.Server.Administration.Logs;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.DimensionalLift;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Database;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Power;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CE.DimensionalLift;

/// <summary>
/// Drives <see cref="CEDimensionalLiftComponent"/>: keeps a linked portal pair alive while the lift is active,
/// with the second portal punched through to the nearest usable z-level below.
/// </summary>
public sealed partial class CEDimensionalLiftSystem : EntitySystem
{
    [Dependency] private CEZLevelsSystem _zLevels = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private LinkedEntitySystem _link = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEDimensionalLiftComponent> ent, ref MapInitEvent args)
    {
        Rebuild(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CEDimensionalLiftComponent> ent, ref PowerChangedEvent args)
    {
        Rebuild(ent);
    }

    [SubscribeLocalEvent]
    private void OnAnchorStateChanged(Entity<CEDimensionalLiftComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            Rebuild(ent);
        else
            Close(ent);
    }

    [SubscribeLocalEvent]
    private void OnZNetworkUpdated(CEZLevelMapNetworkUpdatedEvent args)
    {
        var query = EntityQueryEnumerator<CEDimensionalLiftComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Only lifts standing on a map that belongs to the network that just changed.
            if (Transform(uid).MapUid is not { } mapUid
                || !_zLevels.TryGetMapNetwork(mapUid, out var network)
                || network.Owner != args.Network)
                continue;

            Rebuild((uid, comp));
        }
    }

    /// <summary>
    /// Brings the portal pair in line with the lift's current state: closes it if the lift is inactive,
    /// otherwise recomputes the landing spot and reopens.
    /// </summary>
    private void Rebuild(Entity<CEDimensionalLiftComponent> ent)
    {
        // A stale portal (deleted by a player, map removed, ...) counts as closed.
        if (Deleted(ent.Comp.FirstPortal))
            ent.Comp.FirstPortal = null;
        if (Deleted(ent.Comp.SecondPortal))
            ent.Comp.SecondPortal = null;

        Close(ent, playSound: false);

        // A lift with an ApcPowerReceiver needs power; one without is always active (IsPowered handles both).
        if (!_power.IsPowered(ent))
            return;

        TryOpen(ent);
    }

    private bool TryOpen(Entity<CEDimensionalLiftComponent> ent)
    {
        if (ent.Comp.FirstPortal != null || ent.Comp.SecondPortal != null)
            return false;

        var xform = Transform(ent);
        if (!xform.Anchored)
            return false;

        if (xform.MapUid is not { } mapUid || !TryComp<CEZMapComponent>(mapUid, out var zMap))
            return false;

        var worldPos = _xform.GetWorldPosition(ent);

        if (!TryFindLanding((mapUid, zMap), worldPos, ent.Comp.MaxSearchDepth, out var landing))
            return false;

        var firstCoords = xform.Coordinates;
        var first = Spawn(ent.Comp.FirstPortalPrototype, firstCoords);
        var second = Spawn(ent.Comp.SecondPortalPrototype, landing);

        _link.TryLink(first, second, deleteOnEmptyLinks: true);

        ent.Comp.FirstPortal = first;
        ent.Comp.SecondPortal = second;
        Dirty(ent);

        // Play at coordinates (not the portals) so a later despawn cannot cut the sound short.
        _audio.PlayPvs(ent.Comp.OpenSound, firstCoords);
        _audio.PlayPvs(ent.Comp.OpenSound, landing);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Medium,
            $"{ToPrettyString(ent):lift} opened a dimensional lift portal pair {ToPrettyString(first)} <-> {ToPrettyString(second)} at {Transform(second).Coordinates}");
        return true;
    }

    private void Close(Entity<CEDimensionalLiftComponent> ent, bool playSound = true)
    {
        if (ent.Comp.FirstPortal == null && ent.Comp.SecondPortal == null)
            return;

        ClosePortal(ent.Comp.FirstPortal, ent.Comp.CloseSound, playSound);
        ClosePortal(ent.Comp.SecondPortal, ent.Comp.CloseSound, playSound);

        ent.Comp.FirstPortal = null;
        ent.Comp.SecondPortal = null;
        Dirty(ent);
    }

    private void ClosePortal(EntityUid? portal, SoundSpecifier sound, bool playSound)
    {
        if (Deleted(portal))
            return;

        if (playSound)
            _audio.PlayPvs(sound, Transform(portal.Value).Coordinates);

        QueueDel(portal.Value);
    }

    /// <summary>
    /// Walks the z-stack downward from <paramref name="startMap"/>, returning the coordinates of the first tile
    /// directly under <paramref name="worldPos"/> that exists and is not blocked. Empty space or a blocked tile
    /// on a given level is skipped and the search continues further down.
    /// </summary>
    private bool TryFindLanding(Entity<CEZMapComponent> startMap, Vector2 worldPos, int maxDepth, out EntityCoordinates landing)
    {
        landing = default;

        var current = startMap;
        for (var i = 0; i < maxDepth; i++)
        {
            if (!_zLevels.TryMapDown((current.Owner, current.Comp), out var below))
                return false; // bottom of the stack

            current = below;

            if (!_map.TryFindGridAt(current.Owner, worldPos, out var gridUid, out var grid))
                continue; // nothing to stand on here

            var tile = _map.WorldToTile(gridUid, grid, worldPos);
            if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef) || tileRef.Tile.IsEmpty)
                continue; // no tile

            if (_turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
                continue; // occupied

            landing = _map.ToCoordinates(gridUid, tile, grid);
            return true;
        }

        return false;
    }
}
