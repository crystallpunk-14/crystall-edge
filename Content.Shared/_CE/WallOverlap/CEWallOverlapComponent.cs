using Robust.Shared.Utility;

namespace Content.Shared._CE.WallOverlap;

/// <summary>
/// Makes this wall's sprite spill over onto the neighbouring walls it touches. The neighbours do not store anything:
/// their extra sprite layers are derived on the client from the walls around them, see
/// Content.Client._CE.WallOverlap.CEClientWallOverlapSystem.
///
/// The state must be a directions:4 RSI state authored like wallpaper: the layer on a neighbour sits on the side facing
/// this wall and is relabelled by DirOffset, so the S and N directions stay empty (they are hidden behind this wall)
/// while E is the piece for a neighbour on this wall's west and W the piece for a neighbour on its east.
/// </summary>
[RegisterComponent]
public sealed partial class CEWallOverlapComponent : Component
{
    [DataField(required: true)]
    public ResPath Rsi = default!;

    [DataField(required: true)]
    public string State = string.Empty;

    /// <summary>
    /// IconSmooth keys of the neighbours that get the overlap. Walls with this component never receive it.
    /// </summary>
    [DataField]
    public List<string> TargetKeys = new() { "CEwall" };
}
