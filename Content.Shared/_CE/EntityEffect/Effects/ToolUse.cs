using Content.Shared.Interaction;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class ToolUse : CEEntityEffectBase<ToolUse>
{
    [DataField(required: true)]
    public HashSet<ProtoId<ToolQualityPrototype>> Qualities = new();

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

        if (!TryResolveEffectCoordinates(args.Args, args.Effect.EffectTarget, out var coords))
            return;

        var tool = Spawn(null, Transform(args.Args.Source).Coordinates);
        var toolComp = AddComp<ToolComponent>(tool);
        toolComp.Qualities = args.Effect.Qualities;
        toolComp.SpeedModifier = args.Effect.SpeedModifier;
        EnsureComp<ToolTileCompatibleComponent>(tool);
        EnsureComp<TimedDespawnComponent>(tool).Lifetime = 10f;

        var target = ResolveEffectEntity(args.Args, args.Effect.EffectTarget);
        if (target is { } targetEntity)
            _interaction.InteractUsing(args.Args.Source, tool, targetEntity, coords, checkCanUse: false);
        else
            _interaction.InteractDoAfter(args.Args.Source, tool, null, coords, canReach: true, checkDeletion: false);
    }
}
