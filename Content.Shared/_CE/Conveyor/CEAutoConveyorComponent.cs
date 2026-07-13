using Robust.Shared.GameStates;

namespace Content.Shared._CE.Conveyor;

/// <summary>
/// Marker that auto-activates an existing <see cref="Content.Shared.Conveyor.ConveyorComponent"/>
/// on MapInit (sets state to Forward and powered to true).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEAutoConveyorComponent : Component;
