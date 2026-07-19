namespace Content.Shared._CE.Pen;

/// <summary>
/// Marks an item as a "smart pen": interacting with it on another entity collects available
/// pen actions (writing, recording scientific knowledge, etc.) via <see cref="CEGetPenActionsEvent"/>
/// instead of instantly opening the paper-writing UI.
/// </summary>
[RegisterComponent]
public sealed partial class CEPenComponent : Component
{
    /// <summary>
    /// The entity this pen was last used on, remembered between collecting actions and
    /// resolving the player's choice from the radial menu. Not networked - both client and
    /// server derive it independently from the same predicted interaction.
    /// </summary>
    public EntityUid? PendingTarget;
}
