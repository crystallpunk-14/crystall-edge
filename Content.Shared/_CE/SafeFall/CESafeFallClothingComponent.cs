using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.SafeFall;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CESafeFallClothingSystem))]
public sealed partial class CESafeFallClothingComponent : Component
{
    [DataField]
    public EntProtoId StatusEffect = "CEStatusEffectAirCaught";

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(1);
}
