using Content.Shared._White.AuraImrint;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._White.MagicVision.Components;

/// <summary>
/// Makes you leave random imprints of magical aura instead of the original
/// Use only in conjunction with <see cref="StatusEffectComponent"/>, on the status effect entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(WhiteSharedAuraImprintSystem))]
public sealed partial class WhiteHideMagicAuraStatusEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Imprint = string.Empty;
}
