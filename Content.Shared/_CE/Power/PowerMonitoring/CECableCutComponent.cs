namespace Content.Shared._CE.Power.PowerMonitoring;

/// <summary>
/// Marks an anchored, rotated entity (e.g. <c>CEIsolator</c>) that severs the cable link between its
/// own tile and the tile it faces (<c>LocalRotation.GetCardinalDir()</c>) - the same edge the
/// <c>CableTerminalNode</c> blocks. The CE power monitoring console drops that edge so the map shows
/// the break instead of a continuous run.
/// </summary>
[RegisterComponent]
public sealed partial class CECableCutComponent : Component;
