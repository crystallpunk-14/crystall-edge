using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Drill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CESharedDrillSystem))]
public sealed partial class CEDrillComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    [DataField]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.Impassable | CollisionGroup.MachineMask | CollisionGroup.Opaque);

    [DataField, AutoNetworkedField]
    public TimeSpan NextDamageTime = TimeSpan.Zero;

    [DataField]
    public float JitterAmplitude = 0.5f;

    [DataField]
    public float JitterFreq = 1000f;
}
