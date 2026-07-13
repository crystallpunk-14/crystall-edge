using Content.Shared._CE.MagicEnergy.Systems;
using Content.Shared._CE.Skill;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem : EntitySystem
{
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedHandsSystem _hand = default!;
    [Dependency] private CESharedMagicEnergySystem _magicEnergy = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private CESharedSkillSystem _skill = default!;
    //[Dependency] private CESharedMagicVisionSystem _magicVision = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    private EntityQuery<ActionComponent> _actionQuery;

    public override void Initialize()
    {
        base.Initialize();

        _actionQuery = GetEntityQuery<ActionComponent>();

        InitializeAttempts();
        InitializeExamine();
        InitializePerformed();
        InitializeModularEffects();
        InitializeDoAfter();
    }
}

/// <summary>
/// Called on an action when an attempt to start doAfter using this action begins.
/// </summary>
public sealed class CEActionStartDoAfterEvent(NetEntity performer, RequestPerformActionEvent input) : EntityEventArgs
{
    public NetEntity Performer = performer;
    public readonly RequestPerformActionEvent Input = input;
}
