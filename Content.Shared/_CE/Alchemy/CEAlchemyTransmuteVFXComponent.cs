using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Alchemy;

/// <summary>
/// Flash that appears when a commit brings a mixture into a new zone. The color comes through
/// the appearance data (<see cref="CEAlchemyTransmuteVisuals.Color"/>) so predicted and server entities look the same.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEAlchemyTransmuteVFXComponent : Component;

[Serializable, NetSerializable]
public enum CEAlchemyTransmuteVisuals : byte
{
    Color,
}
