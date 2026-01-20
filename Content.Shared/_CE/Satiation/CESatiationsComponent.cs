using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Satiation;

/// <summary>
///
/// </summary>
[RegisterComponent, Access(typeof(CESharedSatiationSystem)), AutoGenerateComponentPause]
public sealed partial class CESatiationsComponent : Component
{
    [DataField(serverOnly: true)]
    public Dictionary<ProtoId<CESatiationTypePrototype>, float> Satiations = new();

    [DataField(serverOnly: true), AutoPausedField]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;
}
