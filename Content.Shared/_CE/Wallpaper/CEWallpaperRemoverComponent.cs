namespace Content.Shared._CE.Wallpaper;

/// <summary>
/// A tool that strips wallpaper off a wall's CEWallpaperHolderComponent. Like applying wallpaper, only the
/// side the user is interacting from gets cleared.
/// </summary>
[RegisterComponent]
public sealed partial class CEWallpaperRemoverComponent : Component
{
    [DataField]
    public float Delay = 1f;
}
