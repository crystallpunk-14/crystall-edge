using Content.Shared._CE.Actions.Spells;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;

namespace Content.Shared._CE.Actions;

public abstract partial class CESharedActionSystem
{
    private void InitializeModularEffects()
    {
        SubscribeLocalEvent<TransformComponent, CEActionStartDoAfterEvent>(OnActionTelegraphy);

        SubscribeLocalEvent<TransformComponent, CEInstantModularEffectEvent>(OnInstantCast);
        SubscribeLocalEvent<TransformComponent, CEWorldTargetModularEffectEvent>(OnWorldTargetCast);
        SubscribeLocalEvent<TransformComponent, CEEntityTargetModularEffectEvent>(OnEntityTargetCast);
    }

    private void OnActionTelegraphy(Entity<TransformComponent> ent, ref CEActionStartDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var performer = GetEntity(args.Performer);
        var action = GetEntity(args.Input.Action);
        var target = GetEntity(args.Input.EntityTarget);
        var targetPosition = GetCoordinates(args.Input.EntityCoordinatesTarget);

        if (!TryComp<ActionComponent>(action, out var actionComp))
            return;

        //Instant
        if (TryComp<InstantActionComponent>(action, out var instant) && instant.Event is CEInstantModularEffectEvent instantModular)
        {
            var spellArgs = new CESpellEffectBaseArgs(performer, actionComp.Container, performer, Transform(performer).Coordinates);

            foreach (var effect in instantModular.TelegraphyEffects)
            {
                effect.Effect(EntityManager, spellArgs);
            }
        }

        //World Target
        if (TryComp<WorldTargetActionComponent>(action, out var worldTarget) && worldTarget.Event is CEWorldTargetModularEffectEvent worldModular && targetPosition is not null)
        {
            var spellArgs = new CESpellEffectBaseArgs(performer, actionComp.Container, null, targetPosition.Value);

            foreach (var effect in worldModular.TelegraphyEffects)
            {
                effect.Effect(EntityManager, spellArgs);
            }
        }

        //Entity Target
        if (TryComp<EntityTargetActionComponent>(action, out var entityTarget) && entityTarget.Event is CEEntityTargetModularEffectEvent entityModular && target is not null)
        {
            var spellArgs = new CESpellEffectBaseArgs(performer, actionComp.Container, target, Transform(target.Value).Coordinates);

            foreach (var effect in entityModular.TelegraphyEffects)
            {
                effect.Effect(EntityManager, spellArgs);
            }
        }
    }

    private void OnInstantCast(Entity<TransformComponent> ent, ref CEInstantModularEffectEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var spellArgs = new CESpellEffectBaseArgs(args.Performer, args.Action.Comp.Container, args.Performer, Transform(args.Performer).Coordinates);

        foreach (var effect in args.Effects)
        {
            effect.Effect(EntityManager, spellArgs);
        }

        args.Handled = true;
    }

    private void OnWorldTargetCast(Entity<TransformComponent> ent, ref CEWorldTargetModularEffectEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var spellArgs = new CESpellEffectBaseArgs(args.Performer, args.Action.Comp.Container, null, args.Target);

        foreach (var effect in args.Effects)
        {
            effect.Effect(EntityManager, spellArgs);
        }

        args.Handled = true;
    }

    private void OnEntityTargetCast(Entity<TransformComponent> ent, ref CEEntityTargetModularEffectEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var spellArgs = new CESpellEffectBaseArgs(args.Performer, args.Action.Comp.Container, args.Target, Transform(args.Target).Coordinates);

        foreach (var effect in args.Effects)
        {
            effect.Effect(EntityManager, spellArgs);
        }

        args.Handled = true;
    }
}

public sealed partial class CEInstantModularEffectEvent : InstantActionEvent
{
    /// <summary>
    /// Effects that will trigger at the beginning of the cast, before mana is spent. Should have no gameplay importance, just special effects, popups and sounds.
    /// </summary>
    [DataField]
    public List<CESpellEffect> TelegraphyEffects = new();

    [DataField]
    public List<CESpellEffect> Effects = new();
}

public sealed partial class CEWorldTargetModularEffectEvent : WorldTargetActionEvent
{
    /// <summary>
    /// Effects that will trigger at the beginning of the cast, before mana is spent. Should have no gameplay importance, just special effects, popups and sounds.
    /// </summary>
    [DataField]
    public List<CESpellEffect> TelegraphyEffects = new();

    [DataField]
    public List<CESpellEffect> Effects = new();
}

public sealed partial class CEEntityTargetModularEffectEvent : EntityTargetActionEvent
{
    /// <summary>
    /// Effects that will trigger at the beginning of the cast, before mana is spent. Should have no gameplay importance, just special effects, popups and sounds.
    /// </summary>
    [DataField]
    public List<CESpellEffect> TelegraphyEffects = new();

    [DataField]
    public List<CESpellEffect> Effects = new();
}
