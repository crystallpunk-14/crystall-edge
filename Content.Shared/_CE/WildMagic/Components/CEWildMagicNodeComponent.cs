using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.WildMagic.Components;

/// <summary>
/// Wild magic node holding 3 randomly rolled essence aspects, assigned on <see cref="Robust.Shared.GameObjects.MapInitEvent"/>.
/// Each field colors one of the node's sprite layers - see the client-side wild magic system.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEWildMagicNodeComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<CEMagicEssenceTypePrototype> EssenceA;

    [DataField, AutoNetworkedField]
    public ProtoId<CEMagicEssenceTypePrototype> EssenceB;

    [DataField, AutoNetworkedField]
    public ProtoId<CEMagicEssenceTypePrototype> EssenceC;

    /// <summary>
    /// Sprite layer map key that <see cref="EssenceA"/> colors.
    /// </summary>
    [DataField]
    public string EssenceALayer = "essenceA";

    /// <summary>
    /// Sprite layer map key that <see cref="EssenceB"/> colors.
    /// </summary>
    [DataField]
    public string EssenceBLayer = "essenceB";

    /// <summary>
    /// Sprite layer map key that <see cref="EssenceC"/> colors.
    /// </summary>
    [DataField]
    public string EssenceCLayer = "essenceC";
}
