using JetBrains.Annotations;

namespace Content.Shared._CE.Alchemy.Effects;

/// <summary>
/// What a reagent does in alchemy. A reagent lists these in <c>alchemyEffects</c>;
/// an empty list means the reagent is inert: it only adds volume.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CEAlchemyEffect
{
    /// <summary>
    /// Changes the replay state for one recipe step. Volume, path and zones are tracked by the replay itself.
    /// </summary>
    public abstract void Apply(ref CEAlchemyState state, CEAlchemyApplyArgs args);
}
