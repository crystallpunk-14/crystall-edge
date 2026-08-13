using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class AreaEffect : CEEntityEffectBase<AreaEffect>
{
    [DataField(required: true)]
    public List<CEEntityEffect> Effects = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// How many entities can be subject to EntityEffect? Leave 0 to remove the restriction.
    /// </summary>
    [DataField]
    public int MaxTargets;

    [DataField(required: true)]
    public float Range = 1f;

    /// <summary>
    /// Entities closer than this to the area center are excluded - e.g. to skip an item sitting right on
    /// a central pedestal while still hitting everything further out.
    /// </summary>
    [DataField]
    public float MinRange;

    [DataField]
    public bool AffectCaster;

    /// <summary>
    /// When true, entities behind walls (occluded from the area center) will not receive the effect.
    /// </summary>
    [DataField]
    public bool CheckLOS = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Effects.Count == 0)
            return base.EntityEffectGuidebookText(prototype, entSys);

        var nested = string.Join("\n", Effects.Select(e => e.EntityEffectGuidebookText(prototype, entSys)));
        return Loc.GetString("ce-entity-effect-guidebook-area-effect", ("range", Range), ("effects", nested));
    }
}

public sealed partial class CEAreaEffectEffectSystem : CEEntityEffectSystem<AreaEffect>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedTransformSystem _transformSys = default!;

    protected override void Effect(ref CEEntityEffectEvent<AreaEffect> args)
    {
        if (!TryResolveEffectCoordinates(args.Args, args.Effect.EffectTarget, out var targetPoint))
            return;

        var mapCenter = _transformSys.ToMapCoordinates(targetPoint);
        var scaledRange = args.Effect.Range * args.Args.Power;
        var scaledMinRange = args.Effect.MinRange * args.Args.Power;
        var entitiesAround = _lookup.GetEntitiesInRange(targetPoint, scaledRange, LookupFlags.Uncontained);

        var count = 0;
        foreach (var entity in entitiesAround)
        {
            if (entity == args.Args.Source && !args.Effect.AffectCaster)
                continue;

            if (!_whitelist.CheckBoth(entity, args.Effect.Blacklist, args.Effect.Whitelist))
                continue;

            if (scaledMinRange > 0f || args.Effect.CheckLOS)
            {
                var entityMapPos = _transformSys.GetMapCoordinates(entity);

                if (scaledMinRange > 0f && (entityMapPos.Position - mapCenter.Position).Length() < scaledMinRange)
                    continue;

                if (args.Effect.CheckLOS && !_examine.InRangeUnOccluded(mapCenter, entityMapPos, scaledRange, null))
                    continue;
            }

            var nestedArgs = args.Args with { Target = entity, Position = targetPoint };
            foreach (var effect in args.Effect.Effects)
            {
                effect.Effect(nestedArgs);
            }

            count++;

            if (args.Effect.MaxTargets > 0 && count >= args.Effect.MaxTargets)
                break;
        }
    }
}
