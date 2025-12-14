using Content.Shared._CE.Actions.Spells;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Weapons.MeleeEnergyEffect;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class CEMeleeEnergyEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = false;

    [DataField]
    public float EnergyRequired = 10f;

    [DataField]
    public string UseDelayKey = "charging";

    [DataField(required: true)]
    public List<CESpellEffect> Effects = new();

    // Batteries aren't predicted which means we need to track the battery and manually count it ourselves woo!
    [DataField, AutoNetworkedField]
    public int Hits;

    [DataField, AutoNetworkedField]
    public int Capacity;
}
