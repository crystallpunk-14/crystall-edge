using Content.Shared.Damage;
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
    public DamageSpecifier Damage = new()
    {
        DamageDict =
        {
            { "Piercing", 10f },
            { "Structural", 10f },
        },
    };

    [DataField]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.Impassable | CollisionGroup.MachineMask | CollisionGroup.Opaque);

    [DataField]
    public float Distance = 1.0f;

    [DataField]
    public TimeSpan DamageFrequency = TimeSpan.FromSeconds(2f);

    [DataField]
    public TimeSpan NextDamageTime = TimeSpan.Zero;
}
