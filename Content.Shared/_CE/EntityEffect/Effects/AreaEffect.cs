using Content.Shared.Examine;
using Content.Shared.Whitelist;

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

    [DataField]
    public bool AffectCaster;

    /// <summary>
    /// When true, entities behind walls (occluded from the area center) will not receive the effect.
    /// </summary>
    [DataField]
    public bool CheckLOS = true;
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
        var entitiesAround = _lookup.GetEntitiesInRange(targetPoint, scaledRange, LookupFlags.Uncontained);

        var count = 0;
        foreach (var entity in entitiesAround)
        {
            if (entity == args.Args.Source && !args.Effect.AffectCaster)
                continue;

            if (!_whitelist.CheckBoth(entity, args.Effect.Blacklist, args.Effect.Whitelist))
                continue;

            if (args.Effect.CheckLOS)
            {
                var entityMapPos = _transformSys.GetMapCoordinates(entity);
                if (!_examine.InRangeUnOccluded(mapCenter, entityMapPos, scaledRange, null))
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
