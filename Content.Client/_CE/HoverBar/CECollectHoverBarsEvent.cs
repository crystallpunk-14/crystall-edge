namespace Content.Client._CE.HoverBar;

/// <summary>
/// Raised on the hovered entity to collect the bars drawn above it for <see cref="Viewer"/>.
/// HUD systems of the viewer's abilities add their bars while they are active.
/// </summary>
[ByRefEvent]
public readonly record struct CECollectHoverBarsEvent(EntityUid Viewer, List<CEHoverBar> Bars);

/// <summary>
/// One bar above the hovered entity. Layers bottom to top: background, <see cref="Secondary"/>, <see cref="Fill"/>;
/// each layer trails a white impact layer under it.
/// </summary>
/// <param name="Id">Identifies the bar between frames for smooth animation</param>
/// <param name="Order">Bars stack top to bottom by ascending order</param>
/// <param name="Fill">Main fill from 0 to 1</param>
/// <param name="Secondary">Optional layer under the main fill from 0 to 1</param>
public readonly record struct CEHoverBar(
    string Id,
    int Order,
    float Fill,
    Color FillColor,
    float? Secondary = null,
    Color SecondaryColor = default);
