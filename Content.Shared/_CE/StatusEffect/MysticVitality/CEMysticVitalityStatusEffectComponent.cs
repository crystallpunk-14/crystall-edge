using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.StatusEffect.MysticVitality;

/// <summary>
/// Keeps resistable incoming damage from ever reaching the dead threshold. While the target is
/// critical, it also regenerates damage and keeps the target from suffocating.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CEMysticVitalityStatusEffectComponent : Component
{
    /// <summary>
    /// How far below the dead threshold total damage is capped.
    /// </summary>
    [DataField]
    public FixedPoint2 DeathMargin = 1;

    /// <summary>
    /// Damage healed per <see cref="RegenInterval"/> while critical, split proportionally across damage types.
    /// </summary>
    [DataField]
    public FixedPoint2 RegenAmount = 1;

    [DataField]
    public TimeSpan RegenInterval = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRegen = TimeSpan.Zero;

    [DataField]
    public bool PreventCritSuffocation = true;
}
