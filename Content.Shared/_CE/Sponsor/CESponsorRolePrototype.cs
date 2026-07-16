using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Sponsor;

[Prototype("sponsorRole")]
public sealed partial class CESponsorRolePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public string DiscordRoleId = string.Empty;

    [DataField]
    public Color? Color = null;

    [DataField]
    public float Priority = 0;

    [DataField]
    public bool Examinable = false;
}

[Prototype("sponsorFeature")]
public sealed partial class CESponsorFeaturePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    [DataField]
    public float MinPriority = 1;
}