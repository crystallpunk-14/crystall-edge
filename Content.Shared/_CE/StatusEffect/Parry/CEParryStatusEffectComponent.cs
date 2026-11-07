using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.Parry;

/// <summary>
/// Parries the first armed melee attack against the affected entity: the attack misses, the attacker is
/// knocked down and the attacking weapon flies out of their hands. The status ends after one parry.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEParryStatusEffectComponent : Component
{
    [DataField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(0.5f);

    [DataField]
    public float MinThrowDistance = 1f;

    [DataField]
    public float MaxThrowDistance = 2f;

    [DataField]
    public float ThrowSpeed = 6f;

    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("CEParry");

    /// <summary>
    /// Set once the parry is spent, so a second attack in the same tick is not parried before the status is gone.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Spent;
}
