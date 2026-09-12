using Content.Shared._CE.Animation.Core.Prototypes;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.MeleeWeapon;
using Content.Shared.Stealth;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Animation.Item.Components;

/// <summary>
/// Using this item in combat mode triggers action animations on the character.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class CEWeaponComponent : Component
{
    /// <summary>
    /// Mapping from input button to attack action prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<CEUseType, List<CEAnimationEntry>> Animations = new();

    /// <summary>
    /// Named effect slots that animations can reference via <c>WeaponEffectSlot</c>.
    /// Allows weapons to define unique effects (e.g. on-hit, on-cast) without duplicating animations.
    /// </summary>
    [DataField]
    public Dictionary<string, List<CEEntityEffect>> EffectSlots = new();

    /// <summary>
    /// Are we currently holding down the mouse for an attack.
    /// Used so we can't just hold the mouse button and attack constantly.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Using;

    /// <summary>
    /// Which use type the current combo chain belongs to.
    /// Switching to a different use type resets the combo.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CEUseType? LastComboUseType;

    /// <summary>
    /// Next animation index to play in the current combo chain.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ComboIndex;

    /// <summary>
    /// Absolute time after which the combo resets to the first animation.
    /// Calculated as: attack time + animation duration * 1.5 (adjusted for playback speed).
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ComboResetDeadline = TimeSpan.Zero;

    /// <summary>
    /// The sound played on a weapon when it hits something.
    /// </summary>
    [DataField]
    public SoundSpecifier HitSound = new SoundCollectionSpecifier("WeakHit");

    /// <summary>
    /// Base attack range of the weapon in tiles.
    /// WeaponArcAttack multiplies this by its RangeMultiplier to get the effective range per-slot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 1f;

    /// <summary>
    /// Visibility revealed (see <see cref="SharedStealthSystem.ModifyVisibility"/>) on the attacker
    /// whenever a swing with this weapon starts. No-op for entities without a StealthComponent.
    /// </summary>
    [DataField]
    public float StealthRevealOnAttack = 0.5f;
}

[DataDefinition, Serializable]
public sealed partial class CEAnimationEntry
{
    [DataField(required: true)]
    public ProtoId<CEEntityEffectAnimationPrototype> Anim;

    /// <summary>
    /// animation playback speed modifier
    /// </summary>
    [DataField]
    public float Speed = 1f;
}

/// <summary>
/// Which input button binding triggers the attack.
/// </summary>
public enum CEUseType : byte
{
    Primary,
    Secondary,
}
