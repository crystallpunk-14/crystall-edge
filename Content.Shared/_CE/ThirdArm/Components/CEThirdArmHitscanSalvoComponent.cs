using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ThirdArm.Components;

/// <summary>
///     Transient runtime bookkeeping for a burst of hitscan shots still waiting to fire, spaced out over
///     time. Added/removed dynamically by CESharedThirdArmSystem.Hitscan.cs - never present in yaml.
/// </summary>
[RegisterComponent]
public sealed partial class CEThirdArmHitscanSalvoComponent : Component
{
    public List<CEThirdArmScheduledHitscan> Pending = new();
}

public struct CEThirdArmScheduledHitscan
{
    public TimeSpan FireTime;
    public Angle AngleOffset;
    public EntityUid Shooter;
    public EntityCoordinates TargetCoordinates;
    public EntityUid? TargetEntity;
    public EntProtoId Hitscan;
    public SoundSpecifier? Sound;
}
