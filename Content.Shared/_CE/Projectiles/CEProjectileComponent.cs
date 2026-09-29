using Content.Shared._CE.EntityEffect;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Projectiles;

/// <summary>
/// Runs a list of <see cref="CEEntityEffect"/> against whatever the projectile hits,
/// instead of (or in addition to) the vanilla <see cref="Content.Shared.Projectiles.ProjectileComponent.Damage"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEProjectileComponent : Component
{
    [DataField(required: true)]
    public List<CEEntityEffect> HitEffects = new();
}
