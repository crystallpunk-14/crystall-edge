using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.SpellMastery.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.SpellMastery;

// CrystallEdge: rough copy of Content.Shared._CE.InfusionAltar.CEInfusionAltarKnownRecipesEvents,
// not yet cleaned up/unified.

/// <summary>
/// Raised by the client to ask the server for the spell mastery ritual recipes currently known to the
/// local player. Answered with <see cref="CEUpdateSpellMasteryKnownRecipesEvent"/>, sent only to the
/// requesting session so recipe details never leak to other clients.
/// </summary>
[Serializable, NetSerializable]
public sealed class CERequestSpellMasteryKnownRecipesEvent : EntityEventArgs;

/// <summary>
/// Sent by the server, targeted at a single session, in response to
/// <see cref="CERequestSpellMasteryKnownRecipesEvent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEUpdateSpellMasteryKnownRecipesEvent(List<CESpellMasteryKnownRecipeInfo> recipes) : EntityEventArgs
{
    public readonly List<CESpellMasteryKnownRecipeInfo> Recipes = recipes;
}

/// <summary>
/// The revealed, round-rolled requirements of a single spell mastery recipe known to the requesting
/// player. <see cref="Recipe"/> resolves the recipe's static data (result skill) via the already
/// client-synced <see cref="CESpellMasteryRitualRecipePrototype"/>; only the round-random parts are
/// sent here. <see cref="PedestalRequirementIndices"/> are indices into that same prototype's
/// <see cref="CESpellMasteryRitualRecipePrototype.PedestalRequirementPool"/> rather than the rolled
/// <c>CEResourceRequirement</c> instances themselves, since the latter's polymorphic subtypes are not
/// registered with RobustToolbox's NetSerializer.
/// </summary>
[Serializable, NetSerializable]
public sealed class CESpellMasteryKnownRecipeInfo(
    ProtoId<CESpellMasteryRitualRecipePrototype> recipe,
    Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> essences,
    List<int> pedestalRequirementIndices)
{
    public readonly ProtoId<CESpellMasteryRitualRecipePrototype> Recipe = recipe;
    public readonly Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Essences = essences;
    public readonly List<int> PedestalRequirementIndices = pedestalRequirementIndices;
}
