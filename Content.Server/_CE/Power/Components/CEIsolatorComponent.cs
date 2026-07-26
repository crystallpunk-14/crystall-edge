namespace Content.Server._CE.Power.Components;

/// <summary>
/// Marks an entity as a cable isolator. Its rotation is read by <c>CableNode</c> and
/// <c>CEConnectorEdgeNode</c> (via the <c>CableTerminalNode</c> it carries) to sever cable
/// connections in the direction it faces.
/// </summary>
/// <remarks>
/// Placing/removing an isolator doesn't itself join a node group with most neighbors (e.g. valves
/// expose no <c>CableNode</c> for it to touch), so nothing would otherwise force those neighbors to
/// recompute their connections. <see cref="Content.Server._CE.Power.CEIsolatorSystem"/> forces that
/// reflood directly.
/// </remarks>
[RegisterComponent]
public sealed partial class CEIsolatorComponent : Component;
