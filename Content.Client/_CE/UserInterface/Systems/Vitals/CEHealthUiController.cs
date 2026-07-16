using Content.Client._CE.UserInterface.Systems.Vitals.Widgets;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._CE.UserInterface.Systems.Vitals;

[UsedImplicitly]
public sealed partial class CEHealthUiController : UIController
{
    [Dependency] private IPlayerManager _player = default!;

    private CEHealthUI? _healthBar;

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
            UpdateHealth(player);
    }

    private void OnScreenLoad()
    {
        _healthBar = GetHealthBar();

        if (_healthBar == null)
            return;

        if (_player.LocalEntity is { } player)
            UpdateHealth(player);
        else
            _healthBar.Visible = false;
    }

    private void OnScreenUnload()
    {
        if (_healthBar != null)
            _healthBar.Visible = false;

        _healthBar = null;
    }

    private CEHealthUI? GetHealthBar()
    {
        if (UIManager.ActiveScreen is DefaultGameScreen game)
            return game.HealthBar;

        if (UIManager.ActiveScreen is SeparatedChatGameScreen separated)
            return separated.HealthBar;

        return null;
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        _healthBar ??= GetHealthBar();
        UpdateHealth(args.Entity);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (_healthBar != null)
            _healthBar.Visible = false;
    }

    private void UpdateHealth(EntityUid uid)
    {
        if (_healthBar == null)
            return;

        if (_player.LocalEntity is not { } local || uid != local)
        {
            _healthBar.Visible = false;
            return;
        }

        if (!EntityManager.HasComponent<DamageableComponent>(uid) ||
            !EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState))
        {
            _healthBar.Visible = false;
            return;
        }

        var thresholds = EntityManager.System<MobThresholdSystem>();

        if (!thresholds.TryGetIncapThreshold(uid, out var maxHpFixed) ||
            maxHpFixed is not { } maxHpPoint ||
            maxHpPoint <= FixedPoint2.Zero)
        {
            _healthBar.Visible = false;
            return;
        }

        var damageable = EntityManager.System<DamageableSystem>();

        var maxHp = maxHpPoint.Float();
        var totalDamage = damageable.GetTotalDamage(uid).Float();
        var currentHp = MathF.Max(0f, maxHp - totalDamage);
        var ratio = Math.Clamp(currentHp / maxHp, 0f, 1f);
        var critical = mobState.CurrentState != MobState.Alive;

        _healthBar.Visible = true;
        _healthBar.UpdateHealthDisplay(ratio, critical);
    }
}