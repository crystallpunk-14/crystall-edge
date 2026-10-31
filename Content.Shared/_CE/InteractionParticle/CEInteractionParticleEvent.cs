using Robust.Shared.Serialization;

namespace Content.Shared._CE.InteractionParticle;

/// <summary>
/// Plays the "item above the head" particle: a copy of <see cref="Item"/>'s sprite pops up over <see cref="Performer"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEInteractionParticleEvent(NetEntity performer, NetEntity item) : EntityEventArgs
{
    public readonly NetEntity Performer = performer;

    public readonly NetEntity Item = item;
}
