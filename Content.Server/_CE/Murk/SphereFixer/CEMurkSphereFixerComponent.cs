namespace Content.Server._CE.Murk.SphereFixer;

/// <summary>
/// The Pillar of Light: charges up over time and, once full, mends every cracked
/// <c>CEMurkLusconSphereComponent</c> and ends the round. Charging only progresses while
/// <see cref="CEMurkSphereFixerBlockRefreshEvent"/> comes back with no blockers - see
/// <see cref="CEMurkSphereFixerSystem.RefreshBlockConditions"/>.
/// </summary>
[RegisterComponent, Access(typeof(CEMurkSphereFixerSystem), typeof(CEMurkSphereFixerMonitorSystem))]
public sealed partial class CEMurkSphereFixerComponent : Component
{
    /// <summary>
    /// How long it takes to charge from empty to full, assuming no blockers the whole time.
    /// </summary>
    [DataField]
    public TimeSpan ChargeDuration = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Charge fraction accumulated so far, from 0 (empty) to 1 (full).
    /// </summary>
    [DataField]
    public float Charge;

    /// <summary>
    /// Cached result of the last <see cref="CEMurkSphereFixerBlockRefreshEvent"/> broadcast.
    /// Charging is paused (not reset) while this is true.
    /// </summary>
    [DataField]
    public bool Blocked = true;

    /// <summary>
    /// Current blockers (title, description, and problem location) from the last refresh. Read
    /// by <see cref="CEMurkSphereFixerMonitorSystem"/> to feed the monitor console.
    /// </summary>
    public readonly List<CEMurkSphereFixerBlocker> Blockers = new();
}
