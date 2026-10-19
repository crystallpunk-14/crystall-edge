using Robust.Shared.Map;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Raises <see cref="Event"/> directed at the effect's entity, like actions raise their events.
/// Lets any system react to an effect without a dedicated effect type: subscribe to the event
/// on the target's component.
/// </summary>
public sealed partial class RaiseEvent : CEEntityEffectBase<RaiseEvent>
{
    [DataField(required: true)]
    public CEEntityEffectRaisedEvent Event = default!;
}

/// <summary>
/// Base for events raised by the <see cref="RaiseEvent"/> effect. Inheritors are set in YAML via <c>!type:</c>.
/// The context fields are filled in each time the effect fires.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class CEEntityEffectRaisedEvent : HandledEntityEventArgs
{
    /// <summary>
    /// Whoever triggered the effect, e.g. the action's performer.
    /// </summary>
    public EntityUid User;

    /// <summary>
    /// The item the effect was used through, if any.
    /// </summary>
    public EntityUid? Used;

    /// <summary>
    /// The effect's target entity, if any.
    /// </summary>
    public EntityUid? Target;

    /// <summary>
    /// The effect's target position, if any.
    /// </summary>
    public EntityCoordinates? Position;
}

public sealed partial class CERaiseEventEffectSystem : CEEntityEffectSystem<RaiseEvent>
{
    protected override void Effect(ref CEEntityEffectEvent<RaiseEvent> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        var ev = args.Effect.Event;
        ev.Handled = false;
        ev.User = args.Args.Source;
        ev.Used = args.Args.Used;
        ev.Target = args.Args.Target;
        ev.Position = args.Args.Position;

        RaiseLocalEvent(entity, (object) ev);
    }
}
