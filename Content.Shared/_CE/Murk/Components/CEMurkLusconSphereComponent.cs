using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Marks the Lucson Sphere entity that <c>CEMurkConsumingRuleSystem</c> tracks and drains over the round.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEMurkLusconSphereComponent : Component
{
    [DataField, AutoNetworkedField]
    public CEMurkSphereState State = CEMurkSphereState.Stable;

    /// <summary>
    /// How much the sphere's dispel intensity weakens (moves toward 0) each day after cracking.
    /// </summary>
    [DataField]
    public float IntensityPerDay = 5f;

    /// <summary>
    /// How fast the sphere's remaining intensity drains (units/sec) once it starts collapsing.
    /// </summary>
    [DataField]
    public float CollapseRate = 2f;
}

public enum CEMurkSphereState : byte
{
    Stable,
    Cracked,
    Collapsing,
}
