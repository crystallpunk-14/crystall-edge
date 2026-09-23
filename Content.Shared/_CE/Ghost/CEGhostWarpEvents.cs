using Content.Shared._CE.Roles;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Ghost;

/// <summary>
/// CE-owned copy of Content.Shared.Ghost.Systems.GhostWarpsRequestEvent, kept separate so the
/// response can grow CE-specific fields without editing the upstream event/struct.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEGhostWarpsRequestEvent : EntityEventArgs
{
}

/// <summary>
/// CE-owned copy of Content.Shared.Ghost.Systems.GhostWarp.
/// </summary>
[Serializable, NetSerializable]
public struct CEGhostWarp
{
    public CEGhostWarp(
        NetEntity entity,
        string displayName,
        bool isWarpPoint,
        ProtoId<JobPrototype>? job = null,
        ProtoId<CESecretRolePrototype>? secretRole = null)
    {
        Entity = entity;
        DisplayName = displayName;
        IsWarpPoint = isWarpPoint;
        Job = job;
        SecretRole = secretRole;
    }

    /// <summary>
    /// The entity representing the warp point.
    /// This is passed back to the server in the vanilla GhostWarpToTargetRequestEvent.
    /// </summary>
    public NetEntity Entity { get; }

    /// <summary>
    /// The display name to be surfaced in the ghost warps menu.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Whether this warp represents a warp point or a player.
    /// </summary>
    public bool IsWarpPoint { get; }

    /// <summary>
    /// The target's job, if any. Null for warp points and jobless players.
    /// Resolved to an icon/localized name client-side via the prototype manager.
    /// </summary>
    public ProtoId<JobPrototype>? Job { get; }

    /// <summary>
    /// The target's secret role, if any. Null for warp points and players without one.
    /// Shown to every ghost - see CEGhostWarpSystem for the reveal-to-others lookup.
    /// </summary>
    public ProtoId<CESecretRolePrototype>? SecretRole { get; }
}

/// <summary>
/// CE-owned copy of Content.Shared.Ghost.Systems.GhostWarpsResponseEvent.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEGhostWarpsResponseEvent : EntityEventArgs
{
    public CEGhostWarpsResponseEvent(List<CEGhostWarp> warps)
    {
        Warps = warps;
    }

    /// <summary>
    /// A list of warp points.
    /// </summary>
    public List<CEGhostWarp> Warps { get; }
}
