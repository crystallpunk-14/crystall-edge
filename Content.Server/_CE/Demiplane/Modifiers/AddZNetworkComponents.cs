using Robust.Shared.Prototypes;

namespace Content.Server._CE.Demiplane.Modifiers;

/// <summary>
/// Adds <see cref="Components"/> to every map in the z-network while this modifier is active — the
/// same network-wide mechanism <see cref="Prototypes.CEDemiplaneLocationPrototype.Components"/> uses,
/// just picked per-modifier instead of fixed per-location. Not wired up to anything yet.
/// </summary>
public sealed partial class AddZNetworkComponents : ICEDemiplaneModifierEffect
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
