namespace Content.Shared._CE.MagicVision.Components;

/// <summary>
/// Marker for entities that innately, permanently perceive with magic vision (e.g. ghosts) - not
/// contingent on any worn item. Unlike clothing-granted vision, this does not show the client's
/// screen-distorting overlay, since it isn't meant to represent the strain of a worn artifact.
/// </summary>
[RegisterComponent]
public sealed partial class CEPermanentMagicVisionComponent : Component;
