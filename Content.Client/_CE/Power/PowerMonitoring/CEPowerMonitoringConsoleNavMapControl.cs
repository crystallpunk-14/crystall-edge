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

        if (Owner is not { } owner ||
            !_entManager.TryGetComponent<CEPowerMonitoringCableNetworksComponent>(owner, out var cableNetworks))
            return;

        // Copy the networked dictionaries into locals: the component restricts method (Execute)
        // access to the power-monitoring system, but reading the field is allowed.
        var allByGrid = cableNetworks.AllChunks;
        var focusByGrid = cableNetworks.FocusChunks;
        var cutsByGrid = cableNetworks.Cuts;

        foreach (var render in Levels.Values)
        {
            var gridNetEntity = _entManager.GetNetEntity(render.MapUid);

            cutsByGrid.TryGetValue(gridNetEntity, out var cuts);

            if (allByGrid.TryGetValue(gridNetEntity, out var allChunks))
                _allLines[gridNetEntity] = DecodeChunks(allChunks, render.Grid.TileSize, cuts);

            if (focusByGrid.TryGetValue(gridNetEntity, out var focusChunks))
                _focusLines[gridNetEntity] = DecodeChunks(focusChunks, render.Grid.TileSize, cuts);
        }
    }

    private void DrawCablesForLevel(DrawingHandleScreen handle, CEZLevelRender render)
    {
        var gridNetEntity = _entManager.GetNetEntity(render.MapUid);

        var hasFocus = _focusLines.TryGetValue(gridNetEntity, out var focus) && focus.Count > 0;

        if (_allLines.TryGetValue(gridNetEntity, out var all) && all.Count > 0)
            DrawLines(handle, render.Depth, all, hasFocus ? Color.DimGray : Color.White);

        if (hasFocus)
            DrawLines(handle, render.Depth, focus!, Color.White);
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
