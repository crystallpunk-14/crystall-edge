using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Multiplier applied to CESharedZLevelsSystem.ZLevelOffset to get how many tiles of murk
    /// radius one z-level costs. Murk sources are spheres, so this controls how far they punch
    /// through floors and ceilings. Replicated: the client renders the same border the server kills on.
    /// </summary>
    public static readonly CVarDef<float>
        CEMurkZScale = CVarDef.Create("ce.murk.z_scale", 5f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Murk intensity above which an entity counts as being in the murk.
    /// </summary>
    public static readonly CVarDef<float>
        CEMurkThreshold = CVarDef.Create("ce.murk.threshold", 0.5f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Opacity of a single murk layer. Full blackness is reached by stacking several murky z-levels.
    /// </summary>
    public static readonly CVarDef<float>
        CEMurkMaxOpacity = CVarDef.Create("ce.murk.max_opacity", 0.5f, CVar.CLIENT | CVar.ARCHIVE);

    /// <summary>
    /// Exponential smoothing rate for murk intensity changes, in units per second.
    /// </summary>
    public static readonly CVarDef<float>
        CEMurkLerpRate = CVarDef.Create("ce.murk.lerp_rate", 6f, CVar.CLIENT | CVar.ARCHIVE);

    /// <summary>
    /// How far, in tiles, noise is allowed to distort the murk border. Purely cosmetic - the
    /// gameplay border stays a clean circle, so this is also the size of that mismatch.
    /// </summary>
    public static readonly CVarDef<float>
        CEMurkNoiseStrength = CVarDef.Create("ce.murk.noise_strength", 1f, CVar.CLIENT | CVar.ARCHIVE);
}
