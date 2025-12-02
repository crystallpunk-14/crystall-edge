using Content.Shared.Whitelist;
using Robust.Shared.Map;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellArea : CESpellEffect
{
    [DataField(required: true)]
    public List<CESpellEffect> Effects { get; set; } = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// How many entities can be subject to EntityEffect? Leave 0 to remove the restriction.
    /// </summary>
    [DataField]
    public int MaxTargets = 0;

    [DataField(required: true)]
    public float Range = 1f;

    [DataField]
    public bool AffectCaster = false;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        EntityCoordinates? targetPoint = null;

        if (args.Target is not null &&
            entManager.TryGetComponent<TransformComponent>(args.Target.Value, out var transformComponent))
            targetPoint = transformComponent.Coordinates;
        else if (args.Position is not null)
            targetPoint = args.Position;

        if (targetPoint is null)
            return;

        var lookup = entManager.System<EntityLookupSystem>();
        var whitelist = entManager.System<EntityWhitelistSystem>();

        var entitiesAround = lookup.GetEntitiesInRange(targetPoint.Value, Range, LookupFlags.Uncontained);

        var count = 0;
        foreach (var entity in entitiesAround)
        {
            if (entity == args.User && !AffectCaster)
                continue;

            if (!whitelist.CheckBoth(entity, Whitelist, Blacklist))
                continue;

            foreach (var effect in Effects)
            {
                effect.Effect(entManager, new CESpellEffectBaseArgs(args.User, null, entity,  targetPoint));
            }

            count++;

            if (MaxTargets > 0 && count >= MaxTargets)
                break;
        }
    }
}
