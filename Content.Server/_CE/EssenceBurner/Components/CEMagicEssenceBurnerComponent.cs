using Content.Server._CE.EssenceBurner.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Analyzers;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.EssenceBurner.Components;

/// <summary>
/// Continuously burns the reagent in <see cref="Solution"/>: magic essence reagents are converted into
/// charge for the entity's <see cref="Robust.Shared.GameObjects.Component"/> battery, anything else is
/// destroyed and instead builds up <see cref="Instability"/>, which detonates <see cref="ExplosionMishap"/>
/// once it reaches <see cref="MaxInstability"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(CEMagicEssenceBurnerSystem))]
public sealed partial class CEMagicEssenceBurnerComponent : Component
{
    [DataField]
    public string Solution = "essence";

    /// <summary>
    /// Volume of reagent burned per second.
    /// </summary>
    [DataField]
    public FixedPoint2 BurnRate = FixedPoint2.New(1);

    /// <summary>
    /// Battery charge granted per unit of magic essence reagent burned.
    /// </summary>
    [DataField]
    public float EnergyPerUnit = 10f;

    /// <summary>
    /// Current buildup from burning non-essence reagent. Detonates <see cref="ExplosionMishap"/> and
    /// resets to zero on reaching <see cref="MaxInstability"/>.
    /// </summary>
    [DataField]
    public float Instability;

    [DataField]
    public float MaxInstability = 100f;

    /// <summary>
    /// Instability added per unit of non-essence reagent burned.
    /// </summary>
    [DataField]
    public float InstabilityPerUnit = 10f;

    /// <summary>
    /// Instability decay per second, applied every tick regardless of what (if anything) is burning.
    /// </summary>
    [DataField]
    public float InstabilityDecayRate = 1f;

    [DataField]
    public EntProtoId ExplosionMishap = "CEInfusionAltarMishapExplosion";

    [DataField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateFrequency = TimeSpan.FromSeconds(1);
}