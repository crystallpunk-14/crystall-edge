namespace Content.Server._CE.Murk.SphereFixer;

[RegisterComponent, Access(typeof(CEMurkSphereFixerSystem))]
public sealed partial class CEMurkSphereFixerComponent : Component
{
    /// <summary>
    /// How long it takes to charge from empty to full, assuming no blockers the whole time.
    /// </summary>
    [DataField]
    public TimeSpan ChargeDuration = TimeSpan.FromMinutes(10);

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
    /// Human-readable reasons for the current block, from the last refresh. For future UI use.
    /// </summary>
    public readonly List<string> BlockReasons = new();
}
