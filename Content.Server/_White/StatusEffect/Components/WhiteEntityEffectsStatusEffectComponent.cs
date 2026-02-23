using Content.Shared.EntityEffects;

namespace Content.Server._White.StatusEffect.Components;

/// <summary>
/// Applies Entity Effects at a given frequency
/// </summary>
[RegisterComponent, AutoGenerateComponentState, Access(typeof(WhiteEntityEffectsStatusEffectSystem))]

public sealed partial class WhiteEntityEffectsStatusEffectComponent : Component
{
    /// <summary>
    /// List of Effects that will be applied
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = [];

    /// <summary>
    /// How often objects will try to apply <see cref="Effects"/>. In Seconds.
    /// </summary>
    [DataField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The time of the next Effect trigger
    /// </summary>
    [DataField]
    public TimeSpan NextUpdateTime { get; set; } = TimeSpan.Zero;
}
