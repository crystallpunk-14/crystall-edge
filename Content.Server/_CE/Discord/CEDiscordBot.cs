using Content.Server.Discord.DiscordLink;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._CE.Discord;

public sealed partial class CEDiscordBot : IPostInjectInit
{
    [Dependency] private DiscordLink _discordLink = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    public void Initialize()
    {
        _discordLink.OnMessageReceived += AutoReactChivapchichi;
        _discordLink.OnReady += UpdatePlayerCountStatus;
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public void Shutdown()
    {
        _discordLink.OnMessageReceived -= AutoReactChivapchichi;
        _discordLink.OnReady -= UpdatePlayerCountStatus;
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private async void AutoReactChivapchichi(Message message) //  >:)
    {
        if (message.Author.Id != 1049902621630136351)
            return;

        await _discordLink.AddReactionAsync(
            message.ChannelId,
            message.Id,
            new ReactionEmojiProperties("chivapchichi", 1483077159995314226));
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        UpdatePlayerCountStatus();
    }

    private void UpdatePlayerCountStatus()
    {
        var count = _playerManager.PlayerCount;
        var max = _playerManager.MaxPlayers;
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
