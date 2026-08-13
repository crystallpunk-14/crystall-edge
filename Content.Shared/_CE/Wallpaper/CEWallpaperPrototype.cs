using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Wallpaper;

/// <summary>
/// Describes one wallpaper design.
/// Every listed state must be authored as a normal directions:4 RSI state (front/side view, no back frame needed).
/// Which side of the wall a given layer sits on is applied afterwards via a per-layer DirOffset, so a design
/// only ever needs one state per visual variant - never one per wall side.
/// </summary>
[Prototype("ceWallpaper")]
public sealed partial class CEWallpaperPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ResPath RsiPath = default!;

    /// <summary>
    /// One or more directions:4 states for this design. When more than one is given, the client picks a
    /// stable variant per wall+side (deterministic hash, not networked) purely for visual randomization.
    /// </summary>
    [DataField(required: true)]
    public List<string> States = new();
}
