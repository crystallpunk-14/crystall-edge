using Content.Shared._CE.Character;

namespace Content.Client._CE.Character;

/// <summary>
/// Client side of the ghost "view another player's objectives" request. Multiple
/// CECharacterViewWindow instances can be open at once (one per viewed player), each filtering
/// the shared <see cref="Response"/> event by <see cref="CEViewCharacterResponseEvent.Target"/>.
/// </summary>
public sealed partial class CEViewCharacterSystem : EntitySystem
{
    public event Action<CEViewCharacterResponseEvent>? Response;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CEViewCharacterResponseEvent>(OnResponse);
    }

    private void OnResponse(CEViewCharacterResponseEvent msg)
    {
        Response?.Invoke(msg);
    }

    public void RequestView(NetEntity target)
    {
        RaiseNetworkEvent(new CEViewCharacterRequestEvent(target));
    }
}
