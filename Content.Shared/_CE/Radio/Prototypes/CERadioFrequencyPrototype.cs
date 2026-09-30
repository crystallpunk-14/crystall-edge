using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Radio.Prototypes;

/// <summary>
/// City broadcast frequency shared by radio booths and loudspeakers.
/// </summary>
[Prototype("CERadioFrequency")]
public sealed partial class CERadioFrequencyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;
}
