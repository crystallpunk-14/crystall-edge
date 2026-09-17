using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
    /// Time after round start until the sphere cracks and secret role goals are revealed.
    /// </summary>
    [DataField]
    public TimeSpan CrackDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Days (inclusive) the sphere can hold out after cracking before it collapses.
    /// </summary>
    [DataField]
    public int DaysToCollapse = 7;

    /// <summary>
    /// Days passed since the sphere cracked.
    /// </summary>
    [DataField]
    public int DaysSinceCrack;

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

[Serializable, NetSerializable]
public enum CEMurkSphereState : byte
{
    Stable,
    Cracked,
    Collapsing,
    Fixed,
}
