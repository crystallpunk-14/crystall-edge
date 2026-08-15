using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Plays a non-positional, global sound to every listener within [<see cref="MinRange"/>, <see cref="MaxRange"/>]
/// of the effect's coordinates that's on a map belonging to the same Z-level network as the source.
/// Meant for "heard from a distance" flavor sounds (e.g. distant thunder) that should stay reliably
/// audible, since global audio bypasses <c>CEZLevelAudioSystem</c>'s cross-level attenuation entirely.
/// Pair with a <see cref="PlaySound"/> effect for the close/positional variant.
/// </summary>
public sealed partial class PlayFarSound : CEEntityEffectBase<PlayFarSound>
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>
    /// Listeners closer than this (in meters) are excluded — they're expected to already hear a
    /// closer/positional variant of the sound.
    /// </summary>
    [DataField]
    public float MinRange = 15f;

    /// <summary>
    /// Listeners farther than this (in meters) are excluded entirely.
    /// </summary>
    [DataField]
    public float MaxRange = 60f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-play-far-sound");
}

public sealed partial class CEPlayFarSoundEffectSystem : CEEntityEffectSystem<PlayFarSound>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    protected override void Effect(ref CEEntityEffectEvent<PlayFarSound> args)
    {
        if (_net.IsClient)
            return;

        if (!TryResolveEffectCoordinates(args.Args, args.Effect.EffectTarget, out var coords))
            return;

        var sourcePos = TransformSystem.ToMapCoordinates(coords);

        if (!_mapSystem.TryGetMap(sourcePos.MapId, out var sourceMapUid) ||
            !_zLevels.TryGetMapNetwork(sourceMapUid.Value, out var network))
            return;

        var validMaps = new HashSet<MapId>();
        foreach (var mapUid in network.Comp.SortedZLevels)
        {
            if (mapUid.IsValid() && TryComp<MapComponent>(mapUid, out var mapComp))
                validMaps.Add(mapComp.MapId);
        }

        var minRange = args.Effect.MinRange;
        var maxRange = args.Effect.MaxRange;

        var filter = Filter.Broadcast().RemoveWhereAttachedEntity(uid =>
        {
            if (!_xformQuery.TryGetComponent(uid, out var xform) || !validMaps.Contains(xform.MapID))
                return true;

            var distance = (TransformSystem.GetWorldPosition(xform) - sourcePos.Position).Length();
            return distance < minRange || distance > maxRange;
        });

        _audio.PlayGlobal(args.Effect.Sound, filter, true);
    }
}
