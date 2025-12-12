using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.LockKey.Components;

/// <summary>
/// A component of a lock that stores its keyhole shape, complexity, and current state.
/// </summary>
[RegisterComponent, AutoGenerateComponentState(fieldDeltas: true), NetworkedComponent, Access(typeof(CESharedLockKeySystem))]
public sealed partial class CELockComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<int> Shape = new();

    /// <summary>
    /// On which element of the shape sequence the lock is now located. It's necessary for the mechanics of breaking and entering.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int LockPickStatus = 0;

    /// <summary>
    /// If not null, automatically generates a lock for the specified category on initialization. This ensures that the lock will be opened with a key of the same category.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<CELockTypePrototype>? AutoGenerateShape = null;

    /// <summary>
    /// This component is used for two types of items: Entities themselves that are locked (doors, chests),
    /// and a portable lock item that can be built into other entities. This variable determines whether
    /// using this entity on another entity can overwrite the lock properties of the target entity.
    /// </summary>
    [DataField]
    public bool CanEmbedded = false;

    [DataField]
    public SoundSpecifier EmbedSound = new SoundPathSpecifier("/Audio/_CE/Effects/lockpick_use.ogg")
    {
        Params = AudioParams.Default
        .WithVariation(0.05f)
        .WithVolume(0.5f),
    };

    [DataField]
    public TimeSpan EmbedDelay = TimeSpan.FromSeconds(2);
}
