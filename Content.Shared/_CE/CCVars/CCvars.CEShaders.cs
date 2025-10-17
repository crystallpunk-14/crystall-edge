using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool>
        CEWaveShaderEnabled = CVarDef.Create("shaders.ce_wave_shader_enabled", true, CVar.CLIENT | CVar.ARCHIVE);

    /// <summary>
    /// Toggle for non-gameplay-affecting or otherwise status indicative post-process effects, such additive lighting.
    /// In the future, this could probably be turned into an enum that allows only disabling more expensive post-process shaders.
    /// However, for now (mid-July of 2024), this only applies specifically to a particularly cheap shader: additive lighting.
    /// </summary>
    public static readonly CVarDef<bool>
        CEPostProcess = CVarDef.Create("shaders.ce_post_process", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
