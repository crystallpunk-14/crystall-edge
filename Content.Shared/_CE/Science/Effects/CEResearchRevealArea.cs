namespace Content.Shared._CE.Science.Effects;

/// <summary>
/// Reveals a (2 * <see cref="Radius"/> + 1) square centered on the action's coordinate.
/// </summary>
public sealed partial class CEResearchRevealArea : CEResearchActionEffectBase<CEResearchRevealArea>
{
    [DataField]
    public int Radius = 1;
}
