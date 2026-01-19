using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.StationEvents;

/// <summary>
///
/// </summary>
[RegisterComponent]
public sealed partial class CEEntityReplacementRuleComponent : Component
{
    [DataField]
    public Dictionary<EntProtoId, EntProtoId> ReplacementMap = new();

    [DataField]
    public MinMax Range = new(10, 10);

    [DataField]
    public EntProtoId? ReplaceVfx;

    [DataField]
    public SoundSpecifier? ReplaceSound;
}
