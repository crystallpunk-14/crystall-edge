using Content.Shared._CE.EntityEffect;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared._CE.Consumable;

/// <summary>
/// An item applied to oneself or another after a do-after: spends one from its stack, or itself, and applies effects to the target.
/// </summary>
[RegisterComponent, Access(typeof(CEConsumableSystem))]
public sealed partial class CEConsumableComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Multiplies <see cref="Delay"/> when the user applies the item to themselves.
    /// </summary>
    [DataField]
    public float SelfDelayMultiplier = 3f;

    [DataField]
    public bool AllowSelf = true;

    [DataField]
    public bool AllowOthers = true;

    /// <summary>
    /// Keep applying while items remain and <see cref="Conditions"/> pass.
    /// </summary>
    [DataField]
    public bool Repeat;

    /// <summary>
    /// Targets outside the whitelist are silently ignored, so the click falls through to other interactions.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// All must pass to apply; on failure the user sees <see cref="FailPopup"/>.
    /// </summary>
    [DataField]
    public List<CEEntityCondition> Conditions = new();

    [DataField]
    public LocId FailPopup = "ce-consumable-cant-use";

    [DataField]
    public List<CEEntityEffect> Effects = new();

    [DataField]
    public SoundSpecifier? BeginSound;

    [DataField]
    public SoundSpecifier? EndSound;
}
