using Content.Server.Chat.Managers;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.EntityEffect.Effects;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Chat;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._CE.EntityEffect.Effects;

public sealed partial class CEZNetworkMessageEffectSystem : CEEntityEffectSystem<ZNetworkMessage>
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;

    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    protected override void Effect(ref CEEntityEffectEvent<ZNetworkMessage> args)
    {
        if (!TryResolveEffectCoordinates(args.Args, args.Effect.EffectTarget, out var coords))
            return;

        if (TransformSystem.GetMap(coords) is not { } mapUid || !_zLevels.TryGetMapNetwork(mapUid, out var network))
            return;

        var maps = new HashSet<MapId>();
        foreach (var zMap in network.Comp.SortedZLevels)
        {
            if (_mapQuery.TryComp(zMap, out var map))
                maps.Add(map.MapId);
        }

        var filter = Filter.Empty().AddWhereAttachedEntity(uid =>
            _xformQuery.TryComp(uid, out var xform) && maps.Contains(xform.MapID));

        var message = Loc.GetString(args.Effect.Message);
        var wrapped = $"[bold]{FormattedMessage.EscapeText(message)}[/bold]";
        _chat.ChatMessageToManyFiltered(filter, ChatChannel.Server, message, wrapped, default, false, true, args.Effect.Color);
    }
}
