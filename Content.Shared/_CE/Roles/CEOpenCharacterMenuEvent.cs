using Robust.Shared.Serialization;

namespace Content.Shared._CE.Roles;

/// <summary>
/// Sent to a player whenever they are granted a secret role, so the client opens the character menu on the objectives tab.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEOpenCharacterMenuEvent : EntityEventArgs;
