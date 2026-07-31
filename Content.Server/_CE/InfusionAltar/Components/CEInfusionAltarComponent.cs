using Robust.Shared.Timing;

namespace Content.Server._CE.InfusionAltar.Components;

/// <summary>
/// Marks the central pedestal of an infusion altar. While powered, periodically checks whether the
/// single item placed on it (via <see cref="Content.Shared.Placeable.ItemPlacerComponent"/>) plus the
/// essence pooled in <see cref="Solution"/> satisfy any known <see cref="CEInfusionAltarSystem"/> recipe.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(CEInfusionAltarSystem))]
public sealed partial class CEInfusionAltarComponent : Component
{
    /// <summary>
    /// How often to re-check recipe conditions.
    /// </summary>
    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Next time <see cref="CheckInterval"/> allows a recheck.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextCheckTime = TimeSpan.Zero;

    /// <summary>
    /// The solution essence is drained from/into, matching the pedestal's <c>CEMagicEssenceAttractor</c> solution.
    /// </summary>
    [DataField]
    public string Solution = "essence";
}
