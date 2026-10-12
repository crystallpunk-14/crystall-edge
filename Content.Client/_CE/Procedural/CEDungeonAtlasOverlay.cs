using System.Numerics;
using Content.Shared._CE.Procedural;
using Content.Shared._CE.ZLevels.Mapping.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Procedural;

/// <summary>
/// Draws rectangles showing the size and position of each <see cref="CEDungeonRoom3DPrototype"/>
/// that uses the selected <see cref="CEZLevelMapPrototype"/> as its atlas source.
/// </summary>
public sealed partial class CEDungeonAtlasOverlay : Overlay
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IResourceCache _cache = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    /// <summary>
    /// The zMap prototype ID to visualize. Null means no rooms are drawn.
    /// </summary>
    public ProtoId<CEZLevelMapPrototype>? ZMapProtoId;

    private readonly Font _font;

    private readonly List<CEDungeonRoom3DPrototype> _rooms = new();

    public CEDungeonAtlasOverlay()
    {
        IoCManager.InjectDependencies(this);
        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 12);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ZMapProtoId is not { } zMap)
            return;

        _rooms.Clear();
        foreach (var room in _proto.EnumeratePrototypes<CEDungeonRoom3DPrototype>())
        {
            if (room.ZLevelMap == zMap)
                _rooms.Add(room);
        }

        if (args.Space == OverlaySpace.WorldSpace)
            DrawWorld(in args);
        else if (args.Space == OverlaySpace.ScreenSpace)
            DrawScreen(in args);
    }

    private void DrawWorld(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        foreach (var room in _rooms)
        {
            // Offset is the room's bottom-left corner in atlas tile coordinates.
            var box = Box2.FromDimensions(room.Offset, room.Size);
            var color = GetRoomColor(room);

            handle.DrawRect(box, color.WithAlpha(0.3f));
            handle.DrawRect(box, color, filled: false);
        }
    }

    private void DrawScreen(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        if (args.ViewportControl is not { } viewport)
            return;

        foreach (var room in _rooms)
        {
            var screenPos = viewport.WorldToScreen(room.Offset + (Vector2) room.Size / 2f);

            var label = $"{room.ID}\n" +
                        $"size: {room.Size.X}x{room.Size.Y}\n" +
                        $"height: {room.Height}\n" +
                        $"offset: {room.Offset}";

            handle.DrawString(_font, screenPos, label);
        }
    }

    /// <summary>
    /// Distinct hue per room so neighbouring rects are easy to tell apart.
    /// </summary>
    private static Color GetRoomColor(CEDungeonRoom3DPrototype room)
    {
        var hue = (uint) room.ID.GetHashCode() % 360 / 360f;
        return Color.FromHsv(new Vector4(hue, 0.8f, 0.9f, 1f));
    }
}
