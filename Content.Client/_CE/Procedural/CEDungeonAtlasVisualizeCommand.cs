using Content.Shared._CE.Procedural;
using Content.Shared._CE.ZLevels.Mapping.Prototypes;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Procedural;

/// <summary>
/// Toggles the <see cref="CEDungeonAtlasOverlay"/> that visualizes <see cref="CEDungeonRoom3DPrototype"/>
/// rects for a given <see cref="CEZLevelMapPrototype"/>.
/// </summary>
public sealed partial class CEDungeonAtlasVisualizeCommand : LocalizedCommands
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override string Command => "dungen_atlas_visualize";

    public override string Description => "Toggles a debug overlay showing dungeon room rectangles for a zMap prototype.";

    public override string Help => $"Usage: {Command} <zMapProtoId | null>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Help);
            return;
        }

        // No args or "null" — toggle off.
        if (args.Length == 0 || args[0].Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            if (_overlay.RemoveOverlay<CEDungeonAtlasOverlay>())
                shell.WriteLine("Atlas overlay disabled.");
            else
                shell.WriteLine("Atlas overlay is not active.");
            return;
        }

        var protoId = args[0];
        if (!_proto.HasIndex<CEZLevelMapPrototype>(protoId))
        {
            shell.WriteError($"Unknown zMap prototype '{protoId}'.");
            return;
        }

        if (!_overlay.TryGetOverlay<CEDungeonAtlasOverlay>(out var overlay))
        {
            overlay = new CEDungeonAtlasOverlay();
            _overlay.AddOverlay(overlay);
        }

        overlay.ZMapProtoId = protoId;
        shell.WriteLine($"Atlas overlay enabled for '{protoId}'.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<CEZLevelMapPrototype>(proto: _proto),
                "zMap prototype ID");
        }

        return CompletionResult.Empty;
    }
}
