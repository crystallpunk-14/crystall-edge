using System.Linq;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Satiation;

public abstract partial class CESharedSatiationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CESatiationsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CESatiationsComponent> ent, ref MapInitEvent args)
    {
        foreach (var satiation in ent.Comp.Satiations)
        {
            SetSatiationLevel((ent, ent.Comp), satiation.Key, satiation.Value, forceEffectUpdate: true);
        }
    }

    /// <summary>
    /// Adds a new satiation type to an entity with an optional default value.
    /// </summary>
    /// <param name="ent">Entity to add satiation type to</param>
    /// <param name="satiationType">Type of satiation to add</param>
    /// <param name="defaultValue">Initial satiation value. If null, uses the prototype's default value</param>
    /// <returns>True if satiation type was successfully added, false if it already exists or prototype is invalid</returns>
    public void AddSatiationType(EntityUid ent, ProtoId<CESatiationTypePrototype> satiationType, float defaultValue)
    {
        if (_net.IsClient)
            return;

        var satiationComp = EnsureComp<CESatiationsComponent>(ent);

        if (!satiationComp.Satiations.TryAdd(satiationType, defaultValue))
            return;

        SetSatiationLevel((ent, satiationComp), satiationType, defaultValue);
    }

    /// <summary>
    /// Removes a satiation type from an entity and clears any associated status effects.
    /// </summary>
    /// <param name="ent">Entity to remove satiation type from</param>
    /// <param name="satiationType">Type of satiation to remove</param>
    /// <returns>True if satiation type was successfully removed, false if it doesn't exist</returns>
    public void RemoveSatiationType(Entity<CESatiationsComponent?> ent, ProtoId<CESatiationTypePrototype> satiationType)
    {
        if (_net.IsClient)
            return;

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var currentValue))
            return;

        // Remove any active status effect before removing the satiation type
        if (_proto.Resolve(satiationType, out var indexedSatiationType))
        {
            var statusEffect = GetStatusEffectForValue(indexedSatiationType, currentValue);
            if (statusEffect != null)
                _statusEffects.TryRemoveStatusEffect(ent, statusEffect.Value);
        }

        ent.Comp.Satiations.Remove(satiationType);
    }

    /// <summary>
    /// Modifies the satiation value by adding or subtracting a delta amount.
    /// </summary>
    /// <param name="ent">Entity with satiation component</param>
    /// <param name="satiationType">Type of satiation to modify</param>
    /// <param name="delta">Amount to add (positive) or subtract (negative) from current value</param>
    /// <returns>True if value was successfully modified, false otherwise</returns>
    public void EditSatiationLevel(Entity<CESatiationsComponent?> ent, ProtoId<CESatiationTypePrototype> satiationType, float delta)
    {
        if (_net.IsClient)
            return;

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var currentValue))
            return;

        var newValue = currentValue + delta;
        SetSatiationLevel((ent, ent.Comp), satiationType, newValue);
    }

    /// <summary>
    /// Sets the satiation value for a given satiation type and applies appropriate status effects based on thresholds.
    /// </summary>
    /// <param name="ent">Entity with satiation component</param>
    /// <param name="satiationType">Type of satiation to modify</param>
    /// <param name="newValue">New satiation value to set</param>
    /// <returns>True if value was successfully set, false otherwise</returns>
    public void SetSatiationLevel(Entity<CESatiationsComponent?> ent, ProtoId<CESatiationTypePrototype> satiationType, float newValue, bool forceEffectUpdate = false)
    {
        if (_net.IsClient)
            return;

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!_proto.Resolve(satiationType, out var indexedSatiationType))
            return;

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var oldValue))
            return;

        // Clamp value to min/max
        newValue = Math.Clamp(newValue, indexedSatiationType.Min, indexedSatiationType.Max);

        ent.Comp.Satiations[satiationType] = newValue;

        // Determine which status effects should be active for old and new values
        var oldStatusEffect = GetStatusEffectForValue(indexedSatiationType, oldValue);
        var newStatusEffect = GetStatusEffectForValue(indexedSatiationType, newValue);

        // If the status effect has changed, remove the old one and apply the new one
        if (oldStatusEffect != newStatusEffect || forceEffectUpdate)
        {
            // Remove old status effect if it exists
            if (oldStatusEffect != null)
                _statusEffects.TryRemoveStatusEffect(ent, oldStatusEffect.Value);

            // Apply new status effect if it exists (permanent effect with null duration)
            if (newStatusEffect != null)
                _statusEffects.TrySetStatusEffectDuration(ent, newStatusEffect.Value, duration: null);
        }
    }

    /// <summary>
    /// Gets the appropriate status effect for a given satiation value based on thresholds.
    /// </summary>
    /// <param name="satiationType">Satiation type prototype</param>
    /// <param name="value">Current satiation value</param>
    /// <returns>Status effect proto ID or null if no effect should be applied</returns>
    private EntProtoId? GetStatusEffectForValue(CESatiationTypePrototype satiationType, float value)
    {
        if (satiationType.StatusEffectsThresholds.Count == 0)
            return null;

        // Sort thresholds in descending order and find the first one that's <= current value
        var sortedThresholds = satiationType.StatusEffectsThresholds
            .OrderByDescending(kvp => kvp.Key)
            .ToList();

        foreach (var (threshold, effect) in sortedThresholds)
        {
            if (value >= threshold)
                return effect;
        }

        return null;
    }
}
