using Content.Shared._CE.Actions.Spells;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.StatusEffect.SpellApply;

/// <summary>
///     Holds configuration for periodically applying one or more spell effects
///     while a status effect is active on an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CEStatusEffectApplySpellComponent : Component
{
    [DataField(required: true)]
    public List<CESpellEffect> Effects = new();

    [DataField]
    public TimeSpan Frequency = TimeSpan.FromSeconds(1);

    [DataField, AutoPausedField]
    public TimeSpan NextApplyTime = TimeSpan.Zero;
}
