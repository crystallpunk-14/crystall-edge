namespace Content.Server._CE.GameTicking.Components;

/// <summary>
/// Marks the game rule handling the Lucson Sphere's crack/collapse cycle. All sphere-specific
/// config and progress lives on <c>CEMurkLusconSphereComponent</c> instead.
/// </summary>
[RegisterComponent, Access(typeof(CEMurkConsumingRuleSystem))]
public sealed partial class CEMurkConsumingRuleComponent : Component;
