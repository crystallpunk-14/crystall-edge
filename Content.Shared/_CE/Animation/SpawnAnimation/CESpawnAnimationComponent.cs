using Content.Shared._CE.Animation.Core.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Animation.SpawnAnimation;

[RegisterComponent, NetworkedComponent]
public sealed partial class CESpawnAnimationComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CEEntityEffectAnimationPrototype> Animation = string.Empty;
}
