using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.MeleeWeapon;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Reads effects from a named slot on the weapon component and executes them.
/// Allows animations to delegate weapon-specific behavior (projectile type, on-hit effects, etc.)
/// to the weapon definition without duplicating animations.
/// </summary>
public sealed partial class WeaponEffectSlot : CEEntityEffectBase<WeaponEffectSlot>
{
    /// <summary>
    /// The name of the effect slot to read from the weapon's <see cref="CEWeaponComponent.EffectSlots"/>.
    /// </summary>
    [DataField(required: true)]
    public string Slot = string.Empty;

    [DataField]
    public float Power = 1f;
}

public sealed partial class CEWeaponEffectSlotSystem : CEEntityEffectSystem<WeaponEffectSlot>
{
    protected override void Effect(ref CEEntityEffectEvent<WeaponEffectSlot> args)
    {
        if (args.Args.Used is null)
            return;

        if (!TryComp<CEWeaponComponent>(args.Args.Used.Value, out var weapon))
            return;

        var effectsSlots = weapon.EffectSlots;
        if (!effectsSlots.TryGetValue(args.Effect.Slot, out var effects))
            return;

        var scaledArgs = args.Args with { Power = args.Args.Power * args.Effect.Power };
        foreach (var effect in effects)
        {
            effect.Effect(scaledArgs);
        }
    }
}
