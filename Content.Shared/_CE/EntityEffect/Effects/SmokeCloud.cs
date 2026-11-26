using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Releases the contents of a solution of the resolved entity as a spreading smoke cloud at its position.
/// Server-side logic is handled by <c>CESmokeCloudEffectSystem</c>.
/// </summary>
public sealed partial class SmokeCloud : CEEntityEffectBase<SmokeCloud>
{
    /// <summary>
    /// Name of the solution on the resolved entity that fills the cloud.
    /// </summary>
    [DataField]
    public string Solution = "vial";

    [DataField]
    public EntProtoId SmokeProto = "CEAlchemyMist";

    /// <summary>
    /// How long the cloud lingers, in seconds.
    /// </summary>
    [DataField]
    public float Duration = 10f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/smoke.ogg");
}
