using Content.Shared.Examine;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.NewPlayer;

public abstract partial class CESharedNewPlayerSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    private static readonly ProtoId<FactionIconPrototype> Icon = "CENewPlayerIcon";

    [SubscribeLocalEvent]
    private void OnGetStatusIcons(Entity<CENewPlayerComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity == ent.Owner)
            return;

        if (_proto.TryIndex(Icon, out var icon))
            args.StatusIcons.Add(icon);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<CENewPlayerComponent> ent, ref ExaminedEvent args)
    {
        if (args.Examiner == ent.Owner || !CanSee(args.Examiner))
            return;

        // Lowest priority to land at the very end of the examine text.
        var message = new FormattedMessage();
        message.PushNewline();
        message.AddMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString("ce-new-player-examine")));

        args.PushMessage(message, int.MinValue);
    }

    /// <summary>
    /// Whether <paramref name="viewer"/> is allowed to know about new players.
    /// </summary>
    protected virtual bool CanSee(EntityUid viewer)
    {
        return true;
    }
}
