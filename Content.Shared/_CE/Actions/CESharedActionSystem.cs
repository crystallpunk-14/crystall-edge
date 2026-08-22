using Content.Shared._CE.Animation.Core;
using Content.Shared._CE.Animation.Core.Prototypes;
using Content.Shared._CE.MagicEnergy.Systems;
using Content.Shared._CE.MagicFocus.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem : EntitySystem
{
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private CESharedAnimationActionSystem _animation = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedHandsSystem _hand = default!;
    [Dependency] private CESharedMagicEnergySystem _magicEnergy = default!;
    [Dependency] private CEMagicFocusSystem _magicFocus = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    [Dependency] private EntityQuery<ActionComponent> _actionQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeAttempts();
        InitializeExamine();
        InitializePerformed();

        SubscribeLocalEvent<TransformComponent, CEInstantActionAnimationEvent>(OnInstantAction);
        SubscribeLocalEvent<TransformComponent, CEWorldTargetActionAnimationEvent>(OnWorldTargetAction);
        SubscribeLocalEvent<TransformComponent, CEAngleActionAnimationEvent>(OnAngleTargetAction);
        SubscribeLocalEvent<TransformComponent, CEEntityTargetActionAnimationEvent>(OnEntityTargetAction);
    }

    private void OnInstantAction(Entity<TransformComponent> ent, ref CEInstantActionAnimationEvent args)
    {
        if (args.Handled)
            return;

        if (_animation.IsPlayingAnimation(ent))
            return;

        _animation.TryPlayAnimationToAngle(ent, args.Animation, null, args.Action.Comp.Container, args.Speed);
        args.Handled = true;
    }

    private void OnWorldTargetAction(Entity<TransformComponent> ent, ref CEWorldTargetActionAnimationEvent args)
    {
        if (args.Handled)
            return;

        if (_animation.IsPlayingAnimation(ent))
            return;

        // Entity is only set when the action also has EntityTargetAction and the click landed on an entity.
        if (args.Entity is { } entity)
            _animation.TryPlayAnimationToEntity(ent, args.Animation, entity, args.Action.Comp.Container, args.Speed);
        else
            _animation.TryPlayAnimationToCoordinates(ent, args.Animation, args.Target, args.Action.Comp.Container, args.Speed);

        args.Handled = true;
    }

    private void OnAngleTargetAction(Entity<TransformComponent> ent, ref CEAngleActionAnimationEvent args)
    {
        if (args.Handled)
            return;

        if (_animation.IsPlayingAnimation(ent))
            return;

        var playerPos = _transform.GetMapCoordinates(ent).Position;
        var targetPos = _transform.ToMapCoordinates(args.Target).Position;
        var direction = targetPos - playerPos;
        var angle = Angle.FromWorldVec(direction);

        _animation.TryPlayAnimationToAngle(ent, args.Animation, angle, args.Action.Comp.Container, args.Speed);
        args.Handled = true;
    }

    private void OnEntityTargetAction(Entity<TransformComponent> ent, ref CEEntityTargetActionAnimationEvent args)
    {
        if (args.Handled)
            return;

        if (_animation.IsPlayingAnimation(ent))
            return;

        _animation.TryPlayAnimationToEntity(ent, args.Animation, args.Target, args.Action.Comp.Container, args.Speed);
        args.Handled = true;
    }
}


public sealed partial class CEInstantActionAnimationEvent : InstantActionEvent
{
    [DataField(required: true)]
    public ProtoId<CEEntityEffectAnimationPrototype> Animation;

    [DataField]
    public float Speed = 1f;
}

public sealed partial class CEWorldTargetActionAnimationEvent : WorldTargetActionEvent
{
    [DataField(required: true)]
    public ProtoId<CEEntityEffectAnimationPrototype> Animation;

    [DataField]
    public float Speed = 1f;
}

public sealed partial class CEAngleActionAnimationEvent : WorldTargetActionEvent
{
    [DataField(required: true)]
    public ProtoId<CEEntityEffectAnimationPrototype> Animation;

    [DataField]
    public float Speed = 1f;
}


public sealed partial class CEEntityTargetActionAnimationEvent : EntityTargetActionEvent
{
    [DataField(required: true)]
    public ProtoId<CEEntityEffectAnimationPrototype> Animation;

    [DataField]
    public float Speed = 1f;
}



/// <summary>
/// An event that checks all sorts of conditions, and calculates the total cost of casting a spell. Called before the spell is cast.
/// </summary>
/// <remarks>TODO: This call is duplicated at the beginning of the cast for checks, and at the end of the cast for mana subtraction.</remarks>
public sealed partial class CECalculateManacostEvent(EntityUid? performer, int initialManacost) : EntityEventArgs, IInventoryRelayEvent
{
    public EntityUid? Performer = performer;
    public int Manacost = initialManacost;

    public float Multiplier = 1f;

    public int TotalManacost => (int)Math.Ceiling(Manacost * Multiplier);

    public SlotFlags TargetSlots { get; } = SlotFlags.All;
}
