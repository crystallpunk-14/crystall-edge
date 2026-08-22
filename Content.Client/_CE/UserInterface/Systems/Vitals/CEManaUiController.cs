using Content.Client._CE.UserInterface.Systems.Vitals.Widgets;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._CE.UserInterface.Systems.Vitals;

[UsedImplicitly]
public sealed partial class CEManaUiController : UIController
{
    [Dependency] private IPlayerManager _player = default!;

    private SharedBatterySystem? _battery;
    private CEManaUI? _manaBar;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_player.LocalEntity is { } player)
            UpdateMana(player);
    }

    private void OnScreenLoad()
    {
        _manaBar = GetManaBar();

        if (_manaBar == null)
            return;

        if (_player.LocalEntity is { } player)
            UpdateMana(player);
        else
            _manaBar.Visible = false;
    }

    private void OnScreenUnload()
    {
        if (_manaBar != null)
            _manaBar.Visible = false;

        _manaBar = null;
    }

    private CEManaUI? GetManaBar()
    {
        if (UIManager.ActiveScreen is DefaultGameScreen game)
            return game.ManaBar;

        if (UIManager.ActiveScreen is SeparatedChatGameScreen separated)
            return separated.ManaBar;

        return null;
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        _manaBar ??= GetManaBar();
        UpdateMana(args.Entity);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (_manaBar != null)
            _manaBar.Visible = false;
    }

    private void UpdateMana(EntityUid uid, BatteryComponent? container = null)
    {
        _battery ??= EntityManager.System<SharedBatterySystem>();

        if (_manaBar == null)
            return;

        if (_player.LocalEntity is not { } local || uid != local)
        {
            _manaBar.Visible = false;
            return;
        }

        if (container == null && !EntityManager.TryGetComponent(uid, out container))
        {
            _manaBar.Visible = false;
            return;
        }

        if (container.MaxCharge <= 0f)
        {
            _manaBar.Visible = false;
            return;
        }


        var ratio = _battery.GetChargeLevel((uid, container));

        _manaBar.Visible = true;
        _manaBar.SetMana(ratio);
    }
}
