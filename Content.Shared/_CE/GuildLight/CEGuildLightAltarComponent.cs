using Robust.Shared.Prototypes;

namespace Content.Shared._CE.GuildLight;

/// <summary>
/// Creates the guild light at the moment of dawn, if powered. With several altars only one random
/// powered altar creates it, so there is never more than one light.
/// </summary>
[RegisterComponent]
public sealed partial class CEGuildLightAltarComponent : Component
{
    [DataField]
    public EntProtoId Light = "CEGuildLight";
}
