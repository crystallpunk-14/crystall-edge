using Content.Shared._CE.Roundflow;

namespace Content.Client._CE.UserInterface.Systems.Vitals;

/// <summary>
/// Receives <see cref="CERoundProgressStateEvent"/> for <see cref="CERoundProgressUiController"/>.
/// UI controllers subscribe once at client startup and lose their event bus subscriptions on disconnect,
/// while entity systems resubscribe on every connect.
/// </summary>
public sealed partial class CERoundProgressSystem : EntitySystem
{
    public event Action<CERoundProgressStateEvent>? OnProgressState;

    [SubscribeNetworkEvent]
    private void OnProgressStateEvent(CERoundProgressStateEvent ev, EntitySessionEventArgs args)
    {
        OnProgressState?.Invoke(ev);
    }
}
