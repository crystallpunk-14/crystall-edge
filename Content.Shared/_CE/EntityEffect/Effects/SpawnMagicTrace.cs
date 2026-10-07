using Content.Shared._CE.MagicTrace;
using Robust.Shared.Utility;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Leaves a magic trace with the user's aura imprint at the user's position. Always spawned at the user,
/// regardless of <see cref="CEEntityEffect.EffectTarget"/>.
/// </summary>
/// <remarks>
/// Actions with <see cref="Content.Shared._CE.Actions.Components.CEActionManaCostComponent"/> already leave
/// a trace on their own - adding this effect to them results in two traces.
/// </remarks>
public sealed partial class SpawnMagicTrace : CEEntityEffectBase<SpawnMagicTrace>
{
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// Text shown when the trace is examined.
    /// </summary>
    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    public TimeSpan Duration;
}

public sealed partial class CESpawnMagicTraceEffectSystem : CEEntityEffectSystem<SpawnMagicTrace>
{
    [Dependency] private CEMagicTraceSystem _magicTrace = default!;

    protected override void Effect(ref CEEntityEffectEvent<SpawnMagicTrace> args)
    {
        var user = args.Args.Source;

        _magicTrace.SpawnMagicTrace(Transform(user).Coordinates,
            args.Effect.Icon,
            Loc.GetString(args.Effect.Description),
            args.Effect.Duration,
            user);
    }
}
