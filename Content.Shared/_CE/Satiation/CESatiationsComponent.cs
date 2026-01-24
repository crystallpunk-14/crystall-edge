using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Satiation;

/// <summary>
/// Stores satiation values (such as hunger and thirst) for an entity and tracks when they should be updated.
/// </summary>
[RegisterComponent, Access(typeof(CESharedSatiationSystem)), AutoGenerateComponentPause]
public sealed partial class CESatiationsComponent : Component
{
    [DataField(serverOnly: true)]
    public Dictionary<ProtoId<CESatiationTypePrototype>, float> Satiations = new();

    [DataField(serverOnly: true), AutoPausedField]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;
}
