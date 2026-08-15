using Content.Shared.Flash;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Flashes (blinds) the resolved target and, if <see cref="StunDuration"/> is set, stuns them too,
/// via <see cref="SharedFlashSystem.Flash"/>.
/// </summary>
public sealed partial class Flash : CEEntityEffectBase<Flash>
{
    [DataField]
    public TimeSpan FlashDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Movement speed modifier applied while flashed, if not stunned. Between 0 and 1.
    /// </summary>
    [DataField]
    public float SlowTo = 0.5f;

    /// <summary>
    /// If set, the target is stunned (paralyzed) for this long instead of just slowed.
    /// </summary>
    [DataField]
    public TimeSpan? StunDuration;

    [DataField]
    public bool DisplayPopup = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-flash");
}

public sealed partial class CEFlashEffectSystem : CEEntityEffectSystem<Flash>
{
    [Dependency] private SharedFlashSystem _flash = default!;

    protected override void Effect(ref CEEntityEffectEvent<Flash> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        _flash.Flash(entity, args.Args.Source, args.Args.Used, args.Effect.FlashDuration, args.Effect.SlowTo,
            args.Effect.DisplayPopup, stunDuration: args.Effect.StunDuration);
    }
}
