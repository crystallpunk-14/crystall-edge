using Robust.Shared.GameStates;

namespace Content.Shared._CE.ZLevels.Core.Components;

/// <summary>
/// A marker that indicates entities that can actively move between z-levels.
/// </summary>
[RegisterComponent, NetworkedComponent, UnsavedComponent]
public sealed partial class CEActiveZPhysicsComponent : Component;
