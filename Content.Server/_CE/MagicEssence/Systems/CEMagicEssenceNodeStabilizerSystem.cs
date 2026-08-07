using Content.Server.Power.EntitySystems;
using Content.Shared._CE.MagicEssence.Components;
using Content.Shared.Destructible;
using Content.Shared.Power;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.MagicEssence.Systems;

/// <summary>
/// Handles <see cref="CEMagicEssenceNodeStabilizerComponent"/>: whenever the sphere's anchor or
/// power state changes, looks for a <see cref="CEMagicEssenceNodeComponent"/> on the same tile and
/// stops/resumes its time depending on whether the sphere is currently anchored and powered. If the
/// sphere shatters while powered, destroys whatever node it was stabilizing instead of leaving it
/// stuck frozen forever.
/// </summary>
public sealed partial class CEMagicEssenceNodeStabilizerSystem : EntitySystem
{
    [Dependency] private CEMagicEssenceNodeSystem _node = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    private readonly EntProtoId _shatterShockwave = "CEShockWaveWeakVFX";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMagicEssenceNodeStabilizerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<CEMagicEssenceNodeStabilizerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CEMagicEssenceNodeStabilizerComponent, DestructionEventArgs>(OnDestroyed);
    }

    private void OnAnchorChanged(Entity<CEMagicEssenceNodeStabilizerComponent> ent, ref AnchorStateChangedEvent args)
    {
        UpdateStabilizer(ent);
    }

    private void OnPowerChanged(Entity<CEMagicEssenceNodeStabilizerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateStabilizer(ent);
    }

    /// <summary>
    /// If the sphere shatters while it was anchored+powered on a node, that node loses its
    /// protection entirely - destroy it, spawning a shockwave where it stood.
    /// </summary>
    private void OnDestroyed(Entity<CEMagicEssenceNodeStabilizerComponent> ent, ref DestructionEventArgs args)
    {
        if (!this.IsPowered(ent.Owner, EntityManager))
            return;

        var xform = Transform(ent);
        if (FindNodeOnTile(ent, xform) is not { } node)
            return;

        Spawn(_shatterShockwave, xform.Coordinates);
        _node.DestroyNode(node);
    }

    private void UpdateStabilizer(Entity<CEMagicEssenceNodeStabilizerComponent> ent)
    {
        var xform = Transform(ent);

        if (FindNodeOnTile(ent, xform) is not { } node)
            return;

        if (xform.Anchored && this.IsPowered(ent.Owner, EntityManager))
            _node.StopNodeTime(node);
        else
            _node.ResumeNodeTime(node);
    }

    private EntityUid? FindNodeOnTile(EntityUid stabilizer, TransformComponent xform)
    {
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var gridComp))
            return null;

        var tile = _mapSystem.GetTileRef(gridUid, gridComp, xform.Coordinates).GridIndices;

        foreach (var candidate in _mapSystem.GetAnchoredEntities(gridUid, gridComp, tile))
        {
            if (candidate != stabilizer && HasComp<CEMagicEssenceNodeComponent>(candidate))
                return candidate;
        }

        return null;
    }
}
