using Content.Shared._CE.EntityEffect;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.StrapEffect;

/// <summary>
/// A strap that runs a timed cycle on whoever is buckled to it and applies CE effects at the end of each cycle.
/// Cycles repeat while someone stays buckled.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEStrapEffectComponent : Component
{
    /// <summary>
    /// Length of one cycle, from start sound to effects.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Pause after a finished cycle before the next one starts.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Applied to the buckled entity (Target) with the strap as Source and Used.
    /// </summary>
    [DataField]
    public List<CEEntityEffect> Effects = new();

    [DataField]
    public SoundSpecifier? StartSound;

    [DataField]
    public SoundSpecifier? FinishSound;

    /// <summary>
    /// Spawned on the buckled entity when a cycle finishes.
    /// </summary>
    [DataField]
    public EntProtoId? Vfx;

    [DataField]
    public DoAfterId? ActiveDoAfter;

    [DataField, AutoPausedField]
    public TimeSpan NextCycleTime = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public sealed partial class CEStrapEffectDoAfterEvent : SimpleDoAfterEvent;
