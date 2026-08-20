using Content.Shared._CE.Animation.Item.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.MeleeWeapon;

/// <summary>
/// Is called on the object being used to determine what animations it provides
/// </summary>
public sealed class CEGetWeaponAnimationsEvent(Entity<CEWeaponComponent> used, CEUseType useType, EntityUid user) : HandledEntityEventArgs
{
    public Entity<CEWeaponComponent> Used = used;
    public CEUseType UseType = useType;
    public EntityUid User = user;
    public List<CEAnimationEntry> Animations = new();
}

/// <summary>
/// Event raised on entity in GetWeapon function to allow systems to manually
/// specify what the weapon should be.
/// </summary>
public sealed class CEGetWeaponEvent : HandledEntityEventArgs
{
    public Entity<CEWeaponComponent>? Used;
}

/// <summary>
/// Network event sent from client to server when performing an attack.
/// Contains all data needed for both precise and wide attacks.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEWeaponUseEvent(
    Angle angle,
    NetEntity weapon,
    CEUseType useType)
    : EntityEventArgs
{
    /// <summary>
    /// Angle being attacked.
    /// </summary>
    public readonly Angle Angle = angle;

    /// <summary>
    /// The weapon entity being used.
    /// </summary>
    public readonly NetEntity Weapon = weapon;

    /// <summary>
    /// Which button binding triggered this attack.
    /// </summary>
    public readonly CEUseType UseType = useType;
}

/// <summary>
/// Sent from client to server at the arc attack keyframe.
/// Contains the client's calculated hit list for server validation.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEWeaponArcHitEvent(
    NetEntity weapon,
    List<NetEntity> targets,
    string? effectSlot,
    float power = 1f)
    : EntityEventArgs
{
    public readonly NetEntity Weapon = weapon;
    public readonly List<NetEntity> Targets = targets;

    /// <summary>
    /// The EffectSlot key on the weapon that triggered this arc attack.
    /// </summary>
    public readonly string? EffectSlot = effectSlot;

    /// <summary>
    /// Accumulated power multiplier from the WeaponEffectSlot chain.
    /// </summary>
    public readonly float Power = power;
}

/// <summary>
/// Raised when a client releases the attack button.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEStopWeaponUseEvent(NetEntity weapon) : EntityEventArgs
{
    public readonly NetEntity Weapon = weapon;
}

/// <summary>
/// It is called on both the item being used and the creature using the item before the animation starts, to calculate the animation's speed.
/// </summary>
public sealed class CEGetWeaponSpeedEvent : EntityEventArgs
{
    private float _multiplier = 1f;

    public void Modify(float multiplier)
    {
        _multiplier *= multiplier;
    }

    public float GetSpeed()
    {
        return _multiplier;
    }
}

/// <summary>
/// Raised on the weapon entity before a weapon use is committed.
/// Extensibility hook for cost-style components (stamina, mana, etc.) to cancel the attack.
/// No subscribers are ported in this trimmed build.
/// </summary>
public sealed class CEWeaponUseAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User;
    public CEUseType UseType;

    public CEWeaponUseAttemptEvent(EntityUid user, CEUseType useType)
    {
        User = user;
        UseType = useType;
    }
}

/// <summary>
/// Raised on the weapon entity after a weapon use is committed.
/// Extensibility hook for cost-style components to consume resources.
/// No subscribers are ported in this trimmed build.
/// </summary>
public sealed class CEWeaponUsedEvent : EntityEventArgs
{
    public EntityUid User;
    public CEUseType UseType;

    public CEWeaponUsedEvent(EntityUid user, CEUseType useType)
    {
        User = user;
        UseType = useType;
    }
}
