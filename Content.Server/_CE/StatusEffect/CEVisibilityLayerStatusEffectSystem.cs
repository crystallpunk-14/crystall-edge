using Content.Shared._CE.StatusEffect.VisibilityLayer;
using Content.Shared.StatusEffectNew;
using Robust.Server.GameObjects;

namespace Content.Server._CE.StatusEffect;

public sealed partial class CEVisibilityLayerStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private VisibilitySystem _visibility = default!;

    [Dependency] private EntityQuery<VisibilityComponent> _visibilityQuery = default!;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<CEVisibilityLayerStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        var current = _visibilityQuery.TryComp(args.Target, out var visibility) ? visibility.Layer : 0;

        ent.Comp.AddedLayers = ent.Comp.Layers & ~current;
        if (ent.Comp.AddedLayers != 0)
            _visibility.AddLayer(args.Target, (ushort) ent.Comp.AddedLayers);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CEVisibilityLayerStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        var toRemove = ent.Comp.AddedLayers;

        // Already out of the status effect container here, so this only sees the other active effects.
        // Layers they still need are handed over to them instead of being removed.
        foreach (var other in _statusEffects.EnumerateStatusEffects<CEVisibilityLayerStatusEffectComponent>(args.Target))
        {
            var handedOver = toRemove & other.Comp2.Layers;
            other.Comp2.AddedLayers |= handedOver;
            toRemove &= ~handedOver;
        }

        if (toRemove != 0)
            _visibility.RemoveLayer(args.Target, (ushort) toRemove);
    }
}
