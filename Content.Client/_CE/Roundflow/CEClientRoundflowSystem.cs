using Content.Shared._CE.Roundflow;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._CE.RoundFlow;

public sealed class CEClientRoundflowSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private CEScreenPopupControl _ui = default!;
    private bool _remove;

    public override void Initialize()
    {
        SubscribeNetworkEvent<CEScreenPopupShowEvent>(OnScreenPopup);

        _ui = new CEScreenPopupControl();
        _ui.OnAnimationEnd += OnAnimationEnd;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_remove)
            return;

        _userInterface.RootControl.RemoveChild(_ui);
        _remove = false;
    }

    private void OnScreenPopup(CEScreenPopupShowEvent ev)
    {
        if (_player.LocalEntity is null)
            return;

        if (ev.Sound is not null)
            _audio.PlayGlobal(ev.Sound, _player.LocalEntity.Value);

        _ui.AnimationStart(ev);
        _userInterface.RootControl.AddChild(_ui);
    }

    private void OnAnimationEnd()
    {
        _remove = true;
    }
}
