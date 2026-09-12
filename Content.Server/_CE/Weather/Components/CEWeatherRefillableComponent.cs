using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.Weather.Components;

/// <summary>
/// Marks an entity (alongside <see cref="Content.Shared.Nutrition.Components.OpenableComponent"/>) as fillable
/// by weather carrying a <see cref="CEWeatherSolutionFillComponent"/>, while it isn't closed and stands under
/// open sky. Handled by <see cref="CEWeatherSolutionFillSystem"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEWeatherRefillableComponent : Component
{
    /// <summary>
    /// Name of the solution on this entity that weather should fill.
    /// </summary>
    [DataField(required: true)]
    public string Solution = string.Empty;

    [ViewVariables]
    public Entity<SolutionComponent>? SolutionEntity;

    /// <summary>
    /// The next time this container may receive another weather fill cycle.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextFillTime;
}
