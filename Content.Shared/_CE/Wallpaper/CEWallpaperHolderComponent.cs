using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Wallpaper;

/// <summary>
/// Marks a wall as able to hold wallpaper. Lives on the wall entity itself - wallpaper is rendered as
/// extra sprite layers on the wall's own SpriteComponent, not as a separate anchored entity.
///
/// Keyed by cardinal Direction (which edge of the wall tile), one layer per side max - applying wallpaper
/// to a side that already has some replaces it instead of stacking, so unbounded stacking is impossible
/// by construction.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(CESharedWallpaperSystem))]
public sealed partial class CEWallpaperHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<Direction, ProtoId<CEWallpaperPrototype>> Layers = new();
}
