using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Whether the CE GOAP AI system is enabled.
    /// </summary>
    [CVarControl(AdminFlags.VarEdit)]
    public static readonly CVarDef<bool> CEGOAPEnabled =
        CVarDef.Create("ce.goap.enabled", true);

    /// <summary>
    /// Maximum number of GOAP agents updated per tick.
    /// </summary>
    [CVarControl(AdminFlags.VarEdit)]
    public static readonly CVarDef<int> CEGOAPMaxUpdates =
        CVarDef.Create("ce.goap.max_updates", 128);

    /// <summary>
    /// Whether dormant GOAP agents sleep while no player is near. Off: every agent stays awake.
    /// </summary>
    [CVarControl(AdminFlags.VarEdit)]
    public static readonly CVarDef<bool> CEGOAPDormancy =
        CVarDef.Create("ce.goap.dormancy", true, CVar.SERVERONLY);

    /// <summary>
    /// Z-aware distance at which a player wakes a sleeping dormant agent.
    /// </summary>
    [CVarControl(AdminFlags.VarEdit)]
    public static readonly CVarDef<float> CEGOAPWakeRadius =
        CVarDef.Create("ce.goap.wake_radius", 15f, CVar.SERVERONLY);

    /// <summary>
    /// Z-aware distance within which a player keeps an awake dormant agent from falling asleep.
    /// </summary>
    [CVarControl(AdminFlags.VarEdit)]
    public static readonly CVarDef<float> CEGOAPSleepRadius =
        CVarDef.Create("ce.goap.sleep_radius", 25f, CVar.SERVERONLY);

    /// <summary>
    /// Seconds between player proximity checks of dormant agents.
    /// </summary>
    [CVarControl(AdminFlags.VarEdit)]
    public static readonly CVarDef<float> CEGOAPDormancyInterval =
        CVarDef.Create("ce.goap.dormancy_interval", 2f, CVar.SERVERONLY);

    /// <summary>
    /// Seconds without a player inside the sleep radius before an awake dormant agent falls asleep.
    /// </summary>
    [CVarControl(AdminFlags.VarEdit)]
    public static readonly CVarDef<float> CEGOAPSleepDelay =
        CVarDef.Create("ce.goap.sleep_delay", 15f, CVar.SERVERONLY);
}
