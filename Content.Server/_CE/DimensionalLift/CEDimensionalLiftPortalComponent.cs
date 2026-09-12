namespace Content.Server._CE.DimensionalLift;

/// <summary>
/// Marks a portal spawned by a dimensional lift and links it back to the lift that owns it,
/// so <see cref="CEDimensionalLiftSystem"/> can react when something teleports through it.
/// The lift always deletes its portals when it closes or is itself removed (see
/// <see cref="CEDimensionalLiftSystem"/>'s shutdown handler), so this reference never outlives its target.
/// </summary>
/// <remarks>
/// TODO: WeakEntityReference once merged upstream — would let this drop the manual "owner always
/// deletes its portals first" invariant in favor of a self-nulling reference.
/// </remarks>
[RegisterComponent]
public sealed partial class CEDimensionalLiftPortalComponent : Component
{
    [DataField(required: true)]
    public EntityUid Lift;
}
