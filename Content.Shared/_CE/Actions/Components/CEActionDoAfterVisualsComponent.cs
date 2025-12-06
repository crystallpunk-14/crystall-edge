using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions.Components;

/// <summary>
/// Creates a temporary entity that exists while the spell is cast, and disappears at the end. For visual special effects.
/// </summary>
[RegisterComponent, Access(typeof(CESharedActionSystem))]
public sealed partial class CEActionDoAfterVisualsComponent : Component
{
    [DataField]
    public EntityUid? SpawnedEntity;

    [DataField(required: true)]
    public EntProtoId Proto = default!;
}
