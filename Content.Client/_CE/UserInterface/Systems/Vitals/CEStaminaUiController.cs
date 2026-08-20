using Content.Client._CE.UserInterface.Systems.Vitals.Widgets;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._CE.UserInterface.Systems.Vitals;

[UsedImplicitly]
public sealed partial class CEStaminaUiController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private IPlayerManager _player = default!;

    private SharedStaminaSystem? _staminaSystem;
    private CEStaminaUI? _staminaBar;

    public override void Initialize()
    {
        base.Initialize();
        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public void OnStateEntered(GameplayState state)
    {
        _staminaSystem = EntityManager.System<SharedStaminaSystem>();
    }

    public void OnStateExited(GameplayState state)
    {
        _staminaSystem = null;
    }

    private void OnScreenLoad()
    {
        _staminaBar = GetStaminaBar();

        if (_staminaBar == null)
            return;

        if (_player.LocalEntity is { } player)
            UpdateStamina(player);
        else
            _staminaBar.Visible = false;
    }

    private void OnScreenUnload()
    {
        if (_staminaBar != null)
            _staminaBar.Visible = false;

        _staminaBar = null;
    }

    private CEStaminaUI? GetStaminaBar()
    {
        if (UIManager.ActiveScreen is DefaultGameScreen game)
            return game.StaminaBar;

        return null;
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        _staminaBar ??= GetStaminaBar();
        UpdateStamina(args.Entity);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (_staminaBar != null)
            _staminaBar.Visible = false;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_player.LocalEntity is { } player)
            UpdateStamina(player);
    }

    private void UpdateStamina(EntityUid uid)
    {
        if (_staminaBar == null)
            return;

        if (_player.LocalEntity is not { } local || uid != local)
        {
            _staminaBar.Visible = false;
            return;
        }

        if (!EntityManager.TryGetComponent<StaminaComponent>(uid, out var stamina))
        {
            _staminaBar.Visible = false;
            return;
        }

        if (stamina.CritThreshold <= 0f)
        {
            _staminaBar.Visible = false;
            return;
        }

        _staminaBar.Visible = true;

        var damage = _staminaSystem?.GetStaminaDamage(uid, stamina) ?? stamina.StaminaDamage;
        var current = MathF.Max(0f, stamina.CritThreshold - damage);
        var ratio = Math.Clamp(current / stamina.CritThreshold, 0f, 1f);

        _staminaBar.SetStamina(ratio, stamina.Critical);
    }
}