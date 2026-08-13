using System.Linq;
using Content.Server.Discord.DiscordLink;
using Content.Shared.CCVar;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._CE.Discord;

public sealed partial class CEDiscordBot : IPostInjectInit
{
    [Dependency] private DiscordLink _discordLink = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly ulong[] DaNoTargets = [1049902621630136351, 487287488277118986];

    public void Initialize()
    {
        _discordLink.OnMessageReceived += AutoReactDaNo;
        _discordLink.OnReady += UpdatePlayerCountStatus;
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public void Shutdown()
    {
        _discordLink.OnMessageReceived -= AutoReactDaNo;
        _discordLink.OnReady -= UpdatePlayerCountStatus;
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private async void AutoReactDaNo(Message message) // magic 8-ball, kinda
    {
        if (!DaNoTargets.Contains(message.Author.Id))
            return;

        var emoji = _random.Prob(0.5f)
            ? new ReactionEmojiProperties("da", 1532137844343050341)
            : new ReactionEmojiProperties("no", 1532137841419620575);

        await _discordLink.AddReactionAsync(message.ChannelId, message.Id, emoji);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        UpdatePlayerCountStatus();
    }

    private void UpdatePlayerCountStatus()
    {
        var count = _playerManager.PlayerCount;
        var max = _cfg.GetCVar(CCVars.SoftMaxPlayers);
        var text = $"\U0001F465[{count}/{max}]";

        var presence = new PresenceProperties(UserStatusType.Online)
        {
            Activities = new[]
            {
                new UserActivityProperties(text, UserActivityType.Custom)
                {
                    State = text,
                },
            },
        };

        _ = _discordLink.UpdatePresenceAsync(presence);
    }

    void IPostInjectInit.PostInject()
    {
    }
}
