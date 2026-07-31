using Content.Shared._CE.InfusionAltar.Prototypes;
using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.InfusionAltar;

/// <summary>
/// Raised by the client to ask the server for the infusion altar recipes currently known to the local
/// player (round-start recipes plus anything they've been taught). Answered with
/// <see cref="CEUpdateInfusionAltarKnownRecipesEvent"/>, sent only to the requesting session so recipe
/// details never leak to other clients.
/// </summary>
[Serializable, NetSerializable]
public sealed class CERequestInfusionAltarKnownRecipesEvent : EntityEventArgs;

/// <summary>
/// Sent by the server, targeted at a single session, in response to
/// <see cref="CERequestInfusionAltarKnownRecipesEvent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEUpdateInfusionAltarKnownRecipesEvent(List<CEInfusionAltarKnownRecipeInfo> recipes) : EntityEventArgs
{
    public readonly List<CEInfusionAltarKnownRecipeInfo> Recipes = recipes;
}

/// <summary>
/// The revealed, round-rolled requirements of a single infusion altar recipe known to the requesting
/// player. <see cref="Recipe"/> resolves the recipe's static data (catalyst, result) via the already
/// client-synced <see cref="CEInfusionAltarRecipePrototype"/>; only the round-random parts are sent here.
/// <see cref="PedestalRequirementIndices"/> are indices into that same prototype's
/// <see cref="CEInfusionAltarRecipePrototype.PedestalRequirementPool"/> rather than the rolled
/// <c>CEResourceRequirement</c> instances themselves, since the latter's polymorphic subtypes
/// (<c>ProtoIdResource</c>, <c>StackResource</c>, ...) are not registered with RobustToolbox's
/// NetSerializer and throw a <c>KeyNotFoundException</c> if sent directly over the network.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEInfusionAltarKnownRecipeInfo(
    ProtoId<CEInfusionAltarRecipePrototype> recipe,
    Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> essences,
    List<int> pedestalRequirementIndices)
{
    public readonly ProtoId<CEInfusionAltarRecipePrototype> Recipe = recipe;
    public readonly Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Essences = essences;
    public readonly List<int> PedestalRequirementIndices = pedestalRequirementIndices;
}
