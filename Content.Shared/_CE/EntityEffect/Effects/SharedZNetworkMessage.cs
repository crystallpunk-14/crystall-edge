namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Sends a chat message to every player whose attached entity is on a map of the same Z-level network
/// as the effect, ghosts included.
/// </summary>
public sealed partial class ZNetworkMessage : CEEntityEffectBase<ZNetworkMessage>
{
    [DataField(required: true)]
    public LocId Message;

    [DataField]
    public Color Color = Color.White;
}
