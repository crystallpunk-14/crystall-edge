using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.StationEvents;

/// <summary>
/// Defines prototype replacement rules used by station events to swap entities for alternate or damaged versions.
/// This component specifies which entity prototypes can be replaced, how many replacements to perform, and optional
/// visual and audio effects to play when the replacement occurs.
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
