using System.Numerics;
using Content.Client._CE.ZLevels.NavMap;
using Content.Shared._CE.Power.PowerMonitoring;
using Content.Shared.Power;
using Robust.Client.Graphics;
using Robust.Shared.Collections;

namespace Content.Client._CE.Power.PowerMonitoring;

/// <summary>
/// CE fork of <c>Content.Client.Power.PowerMonitoringConsoleNavMapControl</c>. Draws the HV / MV / APC
/// cable networks on top of every drawn z-level of the stacked <see cref="CEZLevelsNavMapControl"/>,
/// reading per-grid chunk data from <see cref="CEPowerMonitoringCableNetworksComponent"/>.
/// </summary>
public sealed partial class CEPowerMonitoringConsoleNavMapControl : CEZLevelsNavMapControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    // Cable indexing matches CableType: 0 = HighVoltage, 1 = MediumVoltage, 2 = Apc
    private static readonly Color[] CableColors = { Color.OrangeRed, Color.Yellow, Color.LimeGreen };
    private static readonly Vector2[] CableOffsets = { new(-0.2f, -0.2f), Vector2.Zero, new(0.2f, 0.2f) };

    /// <summary>Line groups the UI has toggled off.</summary>
    public readonly List<CEPowerMonitoringConsoleLineGroup> HiddenLineGroups = new();

    private readonly Dictionary<NetEntity, List<CEPowerMonitoringConsoleLine>> _allLines = new();
    private readonly Dictionary<NetEntity, List<CEPowerMonitoringConsoleLine>> _focusLines = new();
    private readonly Dictionary<NetEntity, List<CEVerticalPipe>> _verticalPipes = new();

    private readonly Dictionary<Color, Color> _sRgbLookup = new();

    // Scratch dictionaries reused by the per-grid decoder.
    private readonly Dictionary<Vector2i, Vector2i>[] _horizLines = { new(), new(), new() };
    private readonly Dictionary<Vector2i, Vector2i>[] _horizLinesReversed = { new(), new(), new() };
    private readonly Dictionary<Vector2i, Vector2i>[] _vertLines = { new(), new(), new() };
    private readonly Dictionary<Vector2i, Vector2i>[] _vertLinesReversed = { new(), new(), new() };

    public CEPowerMonitoringConsoleNavMapControl()
    {
        IoCManager.InjectDependencies(this);

        TileColor = new Color(58, 100, 128);
        WallColor = new Color(120, 178, 224);

        PostLevelDrawingAction += DrawCablesForLevel;
    }

    protected override void UpdateNavMap()
    {
        base.UpdateNavMap();

        _allLines.Clear();
        _focusLines.Clear();
        _verticalPipes.Clear();

        if (Owner is not { } owner ||
            !_entManager.TryGetComponent<CEPowerMonitoringCableNetworksComponent>(owner, out var cableNetworks))
            return;

        // Copy the networked dictionaries into locals: the component restricts method (Execute)
        // access to the power-monitoring system, but reading the field is allowed.
        var allByGrid = cableNetworks.AllChunks;
        var focusByGrid = cableNetworks.FocusChunks;
        var cutsByGrid = cableNetworks.Cuts;
        var verticalByGrid = cableNetworks.VerticalPipes;

        foreach (var render in Levels.Values)
        {
            var gridNetEntity = _entManager.GetNetEntity(render.MapUid);

            cutsByGrid.TryGetValue(gridNetEntity, out var cuts);

            if (allByGrid.TryGetValue(gridNetEntity, out var allChunks))
                _allLines[gridNetEntity] = DecodeChunks(allChunks, render.Grid.TileSize, cuts);

            if (focusByGrid.TryGetValue(gridNetEntity, out var focusChunks))
                _focusLines[gridNetEntity] = DecodeChunks(focusChunks, render.Grid.TileSize, cuts);

            if (verticalByGrid.TryGetValue(gridNetEntity, out var vertical))
                _verticalPipes[gridNetEntity] = vertical;
        }
    }

    private void DrawCablesForLevel(DrawingHandleScreen handle, CEZLevelRender render)
    {
        var gridNetEntity = _entManager.GetNetEntity(render.MapUid);

        var hasFocus = _focusLines.TryGetValue(gridNetEntity, out var focus) && focus.Count > 0;

        // Down arrows sit under the network...
        DrawVerticalMarkers(handle, render.Depth, render.Grid.TileSize, gridNetEntity, up: false);

        if (_allLines.TryGetValue(gridNetEntity, out var all) && all.Count > 0)
            DrawLines(handle, render.Depth, all, hasFocus ? Color.DimGray : Color.White);

        if (hasFocus)
            DrawLines(handle, render.Depth, focus!, Color.White);

        // ...up arrows over it.
        DrawVerticalMarkers(handle, render.Depth, render.Grid.TileSize, gridNetEntity, up: true);
    }

    /// <summary>
    /// Draws a filled triangle on every pipe that connects to the level <paramref name="up"/> or down.
    /// Its base sits on the cable line's end point (tile centre + that voltage's line offset) and it
    /// extends half a z-level gap screen-up / -down (independent of map rotation). Up arrows are
    /// lighter, down arrows darker.
    /// </summary>
    private void DrawVerticalMarkers(DrawingHandleScreen handle, int depth, int tileSize, NetEntity gridNetEntity, bool up)
    {
        if (!_verticalPipes.TryGetValue(gridNetEntity, out var pipes) || pipes.Count == 0)
            return;

        // Triangle height = half the on-screen gap between adjacent z-levels, kept strictly screen-vertical.
        var height = LevelHeightOffset * MinimapScale * 0.5f;
        var apexOffset = new Vector2(0f, up ? -height : height);
        var width = Math.Clamp(height * 0.7f, 4f, MathF.Max(4f, MinimapScale));

        foreach (var pipe in pipes)
        {
            if (pipe.Voltage >= CableColors.Length)
                continue;

            if (up ? !pipe.Up : !pipe.Down)
                continue;

            var group = (CEPowerMonitoringConsoleLineGroup) pipe.Voltage;
            if (HiddenLineGroups.Contains(group))
                continue;

            var tint = up
                ? Color.InterpolateBetween(CableColors[pipe.Voltage], Color.White, 0.4f)
                : Color.InterpolateBetween(CableColors[pipe.Voltage], Color.Black, 0.4f);

            // Anchor on this voltage's cable line, which is drawn offset from the tile centre so the
            // HV / MV / APC runs don't overlap (see CableOffsets in DrawLines).
            var co = CableOffsets[pipe.Voltage];
            var anchor = LevelToScreen(depth, new Vector2(
                (pipe.Tile.X + 0.5f) * tileSize + co.X,
                (pipe.Tile.Y + 0.5f) * tileSize - co.Y));

            DrawFilledTriangle(handle, anchor, anchor + apexOffset, width, CachedSrgb(tint));
        }
    }

    private static void DrawFilledTriangle(DrawingHandleScreen handle, Vector2 baseCenter, Vector2 apex, float width, Color color)
    {
        var dir = apex - baseCenter;
        if (dir.LengthSquared() < 0.0001f)
            return;

        dir = Vector2.Normalize(dir);
        var perp = new Vector2(-dir.Y, dir.X) * (width * 0.5f);

        var verts = new Vector2[3];
        verts[0] = apex;
        verts[1] = baseCenter + perp;
        verts[2] = baseCenter - perp;

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, verts, color);
    }

    private Color CachedSrgb(Color color)
    {
        if (!_sRgbLookup.TryGetValue(color, out var srgb))
        {
            srgb = Color.ToSrgb(color);
            _sRgbLookup[color] = srgb;
        }

        return srgb;
    }

    private void DrawLines(DrawingHandleScreen handle, int depth, List<CEPowerMonitoringConsoleLine> lines, Color modulate)
    {
        var buckets = new ValueList<Vector2>[3];

        foreach (var line in lines)
        {
            if (HiddenLineGroups.Contains(line.Group))
                continue;

            var idx = (int) line.Group;
            var cableOffset = CableOffsets[idx];

            var origin = line.Origin + cableOffset;
            var terminus = line.Terminus + cableOffset;

            buckets[idx].Add(LevelToScreen(depth, new Vector2(origin.X, -origin.Y)));
            buckets[idx].Add(LevelToScreen(depth, new Vector2(terminus.X, -terminus.Y)));
        }

        for (var i = 0; i < buckets.Length; i++)
        {
            if (buckets[i].Count == 0)
                continue;

            var color = CableColors[i] * modulate;

            if (!_sRgbLookup.TryGetValue(color, out var srgb))
            {
                srgb = Color.ToSrgb(color);
                _sRgbLookup[color] = srgb;
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.LineList, buckets[i].Span, srgb);
        }
    }

    /// <summary>
    /// Ported from <c>PowerMonitoringConsoleNavMapControl.GetDecodedPowerCableChunks</c>. Produces
    /// grid-local, Y-negated line segments (same convention as the base wall geometry).
    /// </summary>
    private List<CEPowerMonitoringConsoleLine> DecodeChunks(Dictionary<Vector2i, PowerCableChunk> chunks, int tileSize, HashSet<CECableCut>? cuts)
    {
        var output = new List<CEPowerMonitoringConsoleLine>();

        Array.ForEach(_horizLines, x => x.Clear());
        Array.ForEach(_horizLinesReversed, x => x.Clear());
        Array.ForEach(_vertLines, x => x.Clear());
        Array.ForEach(_vertLinesReversed, x => x.Clear());

        const int chunkSize = CESharedPowerMonitoringConsoleSystem.ChunkSize;

        foreach (var (chunkOrigin, chunk) in chunks)
        {
            for (var cableIdx = 0; cableIdx < 3; cableIdx++)
            {
                var horizLines = _horizLines[cableIdx];
                var horizLinesReversed = _horizLinesReversed[cableIdx];
                var vertLines = _vertLines[cableIdx];
                var vertLinesReversed = _vertLinesReversed[cableIdx];

                var chunkMask = chunk.PowerCableData[cableIdx];

                for (var chunkIdx = 0; chunkIdx < chunkSize * chunkSize; chunkIdx++)
                {
                    if ((chunkMask & (1 << chunkIdx)) == 0x0)
                        continue;

                    var relativeTile = CESharedPowerMonitoringConsoleSystem.GetTileFromIndex(chunkIdx);
                    var gridTile = chunk.Origin * chunkSize + relativeTile;
                    var tile = gridTile * tileSize;
                    tile = tile with { Y = -tile.Y };

                    bool neighbor;

                    // East neighbour
                    if (relativeTile.X == chunkSize - 1)
                    {
                        neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(1, 0), out var neighborChunk) &&
                                   (neighborChunk.PowerCableData[cableIdx] & CESharedPowerMonitoringConsoleSystem.GetFlag(new Vector2i(0, relativeTile.Y))) != 0x0;
                    }
                    else
                    {
                        neighbor = (chunkMask & CESharedPowerMonitoringConsoleSystem.GetFlag(relativeTile + new Vector2i(1, 0))) != 0x0;
                    }

                    if (neighbor && !IsCut(cuts, gridTile, gridTile + new Vector2i(1, 0)))
                        CENavMapGeometry.AddOrUpdateNavMapLine(tile, tile + new Vector2i(tileSize, 0), horizLines, horizLinesReversed);

                    // North neighbour
                    if (relativeTile.Y == chunkSize - 1)
                    {
                        neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(0, 1), out var neighborChunk) &&
                                   (neighborChunk.PowerCableData[cableIdx] & CESharedPowerMonitoringConsoleSystem.GetFlag(new Vector2i(relativeTile.X, 0))) != 0x0;
                    }
                    else
                    {
                        neighbor = (chunkMask & CESharedPowerMonitoringConsoleSystem.GetFlag(relativeTile + new Vector2i(0, 1))) != 0x0;
                    }

                    if (neighbor && !IsCut(cuts, gridTile, gridTile + new Vector2i(0, 1)))
                        CENavMapGeometry.AddOrUpdateNavMapLine(tile + new Vector2i(0, -tileSize), tile, vertLines, vertLinesReversed);
                }
            }
        }

        var gridOffset = new Vector2(tileSize * 0.5f, -tileSize * 0.5f);

        for (var index = 0; index < _horizLines.Length; index++)
        {
            foreach (var (origin, terminal) in _horizLines[index])
                output.Add(new CEPowerMonitoringConsoleLine(origin + gridOffset, terminal + gridOffset, (CEPowerMonitoringConsoleLineGroup) index));
        }

        for (var index = 0; index < _vertLines.Length; index++)
        {
            foreach (var (origin, terminal) in _vertLines[index])
                output.Add(new CEPowerMonitoringConsoleLine(origin + gridOffset, terminal + gridOffset, (CEPowerMonitoringConsoleLineGroup) index));
        }

        return output;
    }

    private static bool IsCut(HashSet<CECableCut>? cuts, Vector2i a, Vector2i b)
    {
        return cuts != null && cuts.Contains(new CECableCut(a, b));
    }
}

public readonly struct CEPowerMonitoringConsoleLine
{
    public readonly Vector2 Origin;
    public readonly Vector2 Terminus;
    public readonly CEPowerMonitoringConsoleLineGroup Group;

    public CEPowerMonitoringConsoleLine(Vector2 origin, Vector2 terminus, CEPowerMonitoringConsoleLineGroup group)
    {
        Origin = origin;
        Terminus = terminus;
        Group = group;
    }
}

public enum CEPowerMonitoringConsoleLineGroup : byte
{
    HighVoltage,
    MediumVoltage,
    Apc,
}
