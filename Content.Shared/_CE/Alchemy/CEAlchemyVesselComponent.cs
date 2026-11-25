using Robust.Shared.GameStates;

namespace Content.Shared._CE.Alchemy;

/// <summary>
/// A container whose mixable solution can be committed into an alchemy mixture: by shaking it
/// (needs <c>Shakeable</c>) or by stirring it with a <c>ReactionMixer</c> such as a spoon.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEAlchemyVesselComponent : Component;
