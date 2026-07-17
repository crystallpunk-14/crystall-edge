using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether players must link + verify Discord (via TypeAuth) before joining the game.
    /// </summary>
    public static readonly CVarDef<bool> CEDiscordAuthEnabled =
        CVarDef.Create("ce.discord_auth_enabled", false, CVar.SERVERONLY);

    /// <summary>
    ///     Base URL of the TypeAuth service used for both the Discord-auth gate and sponsor role lookups.
    /// </summary>
    public static readonly CVarDef<string> CETypeAuthUrl =
        CVarDef.Create("ce.typeauth_url", "http://localhost:2424", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Bearer token used to authenticate against the TypeAuth service.
    /// </summary>
    public static readonly CVarDef<string> CETypeAuthToken =
        CVarDef.Create("ce.typeauth_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Whether Discord-sponsor role lookups (OOC color, feature unlocks, priority join) are active.
    /// </summary>
    public static readonly CVarDef<bool> CESponsorEnabled =
        CVarDef.Create("ce.sponsor_enabled", false, CVar.SERVERONLY);
}