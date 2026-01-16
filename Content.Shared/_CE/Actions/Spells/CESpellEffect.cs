using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared._CE.Actions.Spells;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CESpellEffect
{
    public abstract void Effect(EntityManager entManager, CESpellEffectBaseArgs args);
}

public record CESpellEffectBaseArgs(EntityUid? User, EntityUid? Used, EntityUid? Target, EntityCoordinates? Position);
