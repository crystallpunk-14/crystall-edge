namespace Content.Shared._CE.Power.PowerMonitoring;

/// <summary>
/// Marker for a CE power line (brass pipe carrying a <c>CableComponent</c>). Lets the CE power
/// monitoring console maintain its per-grid cable chunk cache via anchor events on THIS component,
/// avoiding a duplicate subscription clash with the upstream <c>PowerMonitoringConsoleSystem</c>,
/// which already owns <c>&lt;CableComponent, CableAnchorStateChangedEvent&gt;</c>.
/// </summary>
[RegisterComponent]
public sealed partial class CECableComponent : Component;
