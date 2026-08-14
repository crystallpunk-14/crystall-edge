using Content.Shared.Interaction;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Utility;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
///     Performs an InteractUsing (e.g. a tool click) from the effect's source onto the resolved target entity,
///     using a throwaway entity with the given tool qualities. The tool never exists as a real, clickable item,
///     so the qualities can never be triggered except through this effect (e.g. a mana-gated action).
///     The throwaway entity outlives the call via <see cref="TimedDespawnComponent"/> so it stays valid for
///     any DoAfter the interaction starts (tool quality is only checked when the DoAfter starts, not when it
///     finishes, so this is safe).
/// </summary>
public sealed partial class ToolUse : CEEntityEffectBase<ToolUse>
{
    [DataField(required: true)]
    public PrototypeFlags<ToolQualityPrototype> Qualities = new();

    [DataField]
    public float SpeedModifier = 1f;
}

public sealed partial class CEToolUseEffectSystem : CEEntityEffectSystem<ToolUse>
{
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private INetManager _net = default!;

    protected override void Effect(ref CEEntityEffectEvent<ToolUse> args)
    {
        if (!_net.IsServer)
            return;

        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } target)
            return;

        if (!TryResolveEffectCoordinates(args.Args, args.Effect.EffectTarget, out var coords))
            return;

        var tool = Spawn(null, Transform(args.Args.Source).Coordinates);
        var toolComp = AddComp<ToolComponent>(tool);
        toolComp.Qualities = args.Effect.Qualities;
        toolComp.SpeedModifier = args.Effect.SpeedModifier;
        EnsureComp<TimedDespawnComponent>(tool).Lifetime = 10f;

        _interaction.InteractUsing(args.Args.Source, tool, target, coords, checkCanUse: false);
    }
}
