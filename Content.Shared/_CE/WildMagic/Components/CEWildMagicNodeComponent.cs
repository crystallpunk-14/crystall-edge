using Content.Shared._CE.WildMagic.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.WildMagic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEWildMagicNodeComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEWildMagicTypePrototype>, float> Types = new();

    /// <summary>
    /// Client-side only. Tracks the sprite layer keys currently added from <see cref="Types"/>, so
    /// they can be removed before the visuals are rebuilt.
    /// </summary>
    public HashSet<string> RevealedLayers = new();
}
