using Content.Server.Chat.Systems;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server._CE.ZLevels.Chat;

public sealed partial class CEZLevelSpeakingSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly CESharedZLevelsSystem _zLevel = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<MapComponent> _mapQuery;

    public override void Initialize()
    {
        base.Initialize();

        _mapQuery = GetEntityQuery<MapComponent>();

        SubscribeLocalEvent<CEZLevelViewerComponent, EntitySpokeEvent>(OnSpoke);
    }

    private void OnSpoke(Entity<CEZLevelViewerComponent> ent, ref EntitySpokeEvent args)
    {
        var xform = Transform(ent);
        var sourceMap = xform.MapUid;
        if (sourceMap is null)
            return;

        var globalPosition = _transform.GetWorldPosition(xform);
        var message = args.Message;

        //Try transmit message to 1 zlevel down
        if (_zLevel.TryMapDown(sourceMap.Value, out var belowMapUid) &&
            _mapQuery.TryComp(belowMapUid, out var belowMapComp))
        {
            var targetPos = new MapCoordinates(globalPosition, belowMapComp.MapId);
            var transmit = Spawn(null, targetPos);
            EnsureComp<TimedDespawnComponent>(transmit).Lifetime = 3f;

            Timer.Spawn(333,
                () =>
                {
                    _chat.TrySendInGameICMessage(
                        transmit,
                        message,
                        InGameICChatType.Whisper,
                        false,
                        nameOverride: "From up",
                        ignoreActionBlocker: true);
                });
        }

        //Try transmit message to 1 zlevel up
        if (_zLevel.TryMapUp(sourceMap.Value, out var aboveMapUid) &&
            _mapQuery.TryComp(aboveMapUid, out var aboveMapComp))
        {
            var targetPos = new MapCoordinates(globalPosition, aboveMapComp.MapId);
            var transmit = Spawn(null, targetPos);
            EnsureComp<TimedDespawnComponent>(transmit).Lifetime = 3f;

            Timer.Spawn(333,
                () =>
                {
                    _chat.TrySendInGameICMessage(
                        transmit,
                        message,
                        InGameICChatType.Whisper,
                        false,
                        nameOverride: "From down",
                        ignoreActionBlocker: true);
                });
        }
    }
}
