using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.StatusEffect.VisibilityLayer;

/// <summary>
/// Adds visibility layers to the affected entity while the status effect lasts, so only viewers whose
/// visibility mask has all of them keep seeing it. Once the effect ends, only the layers it actually added
/// are removed - layers the entity already had stay, and so do layers another active effect still needs.
/// </summary>
[RegisterComponent]
public sealed partial class CEVisibilityLayerStatusEffectComponent : Component
{
    [DataField(customTypeSerializer: typeof(FlagSerializer<VisibilityMaskLayer>))]
    public int Layers;

    /// <summary>
    /// Layers this effect is responsible for removing - the ones the entity didn't have when it was applied,
    /// plus any handed over by an effect of this kind that ended while they were still needed here.
    /// </summary>
    [DataField]
    public int AddedLayers;
}
