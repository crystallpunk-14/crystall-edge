using Content.Shared._CE.DiscordAuth;
using Robust.Client.State;
using Robust.Shared.Network;

namespace Content.Client._CE.DiscordAuth;

public sealed partial class CEDiscordAuthManager
{
    [Dependency] private IClientNetManager _netManager = default!;
    [Dependency] private IStateManager _stateManager = default!;

    public string AuthUrl { get; private set; } = "";
    public string ErrorMessage { get; private set; } = "";

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgCEDiscordAuthCheck>();
        _netManager.RegisterNetMessage<MsgCEDiscordAuthRequired>(OnDiscordAuthRequired);
    }

    private void OnDiscordAuthRequired(MsgCEDiscordAuthRequired msg)
    {
        if (_stateManager.CurrentState is CEDiscordAuthState)
            return;
        AuthUrl = msg.AuthUrl;
        ErrorMessage = msg.ErrorMessage;
        _stateManager.RequestStateChange<CEDiscordAuthState>();
    }
}
