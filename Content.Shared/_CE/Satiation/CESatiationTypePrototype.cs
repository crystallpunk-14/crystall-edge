using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Satiation;

[Prototype("satiationType")]
public sealed partial class CESatiationTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public float Min = 0;

    [DataField]
    public float Max = 100;

    /// <summary>
    /// Decaying per second
    /// </summary>
    [DataField]
    public float DecayRate = 0.001f;

    /// <summary>
    /// What status effect should be applied to the entity, at what saturation level?
    /// </summary>
    /// <remarks>
    /// Fills with the status effect from the specified value until the next entry.
    /// That is, if you specify, for example, [10, Effect1], Effect1 will be applied to the entity starting from 10 saturation and above to infinity.
    /// </remarks>
    [DataField]
    public Dictionary<float, EntProtoId?> StatusEffectsThresholds = new();
}
