using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Wallpaper;

/// <summary>
/// A roll of wallpaper. Applied to a wall's CEWallpaperHolderComponent via CESharedWallpaperSystem.
/// </summary>
[RegisterComponent]
public sealed partial class CEWallpaperComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CEWallpaperPrototype> Proto;

    [DataField]
    public float Delay = 1f;
}
