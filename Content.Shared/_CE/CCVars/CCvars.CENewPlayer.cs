using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Players with less overall playtime than this (in hours) get the new player icon above their head. 0 disables it.
    /// </summary>
    [CVarControl(AdminFlags.Admin)]
    public static readonly CVarDef<float> CENewPlayerPlaytimeHours =
        CVarDef.Create("ce.new_player_playtime_hours", 3f, CVar.SERVERONLY);
}
