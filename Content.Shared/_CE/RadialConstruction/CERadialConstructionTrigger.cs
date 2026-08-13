using Content.Shared._CE.ResourceManager;
using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.RadialConstruction;

/// <summary>
/// Describes what a player must interact with an entity's <see cref="CERadialConstructionComponent"/> variant
/// with to open its radial menu, and what happens to that item once a choice from the menu is confirmed.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CERadialConstructionTrigger
{
    /// <summary>
    /// Whether the given held item can activate this trigger.
    /// </summary>
    public abstract bool Matches(IEntityManager entManager, IPrototypeManager protoManager, EntityUid item);

    /// <summary>
    /// Starts whatever interaction (tool-use bar, plain do-after, ...) has to finish before the radial choice is
    /// committed. <paramref name="doAfterEv"/> must eventually be raised on <paramref name="target"/>.
    /// </summary>
    public abstract bool StartUse(IEntityManager entManager, EntityUid item, EntityUid user, EntityUid target, float delay, DoAfterEvent doAfterEv);

    /// <summary>
    /// Called once the do-after finishes successfully, right before the target prototype is spawned.
    /// Consume <paramref name="item"/> here if this trigger is meant to be spent by the interaction.
    /// </summary>
    public virtual void Commit(IEntityManager entManager, IPrototypeManager protoManager, EntityUid item)
    {
    }

    /// <summary>
    /// Text shown on examine to hint what this trigger needs. Return null to omit it.
    /// </summary>
    public abstract string? GetExamineHint(IPrototypeManager protoManager);
}

/// <summary>
/// Fires when a tool with the required quality is used on the entity. The tool itself is never consumed.
/// </summary>
public sealed partial class CERadialToolTrigger : CERadialConstructionTrigger
{
    [DataField(required: true)]
    public ProtoId<ToolQualityPrototype> Quality;

    public override bool Matches(IEntityManager entManager, IPrototypeManager protoManager, EntityUid item)
    {
        return entManager.System<SharedToolSystem>().HasQuality(item, Quality);
    }

    public override bool StartUse(IEntityManager entManager, EntityUid item, EntityUid user, EntityUid target, float delay, DoAfterEvent doAfterEv)
    {
        return entManager.System<SharedToolSystem>().UseTool(item, user, target, delay, Quality, doAfterEv);
    }

    public override string? GetExamineHint(IPrototypeManager protoManager)
    {
        return protoManager.TryIndex(Quality, out var quality) ? Loc.GetString(quality.ToolName) : null;
    }
}

/// <summary>
/// Fires when an item satisfying a <see cref="CEResourceRequirement"/> (a material stack, a specific
/// entity, ...) is used on the entity. The item is spent via <see cref="CEResourceRequirement.PostCraft"/>.
/// </summary>
public sealed partial class CERadialRequirementTrigger : CERadialConstructionTrigger
{
    [DataField(required: true)]
    public CEResourceRequirement Requirement = default!;

    public override bool Matches(IEntityManager entManager, IPrototypeManager protoManager, EntityUid item)
    {
        return Requirement.CheckRequirement(entManager, protoManager, new HashSet<EntityUid> { item });
    }

    public override bool StartUse(IEntityManager entManager, EntityUid item, EntityUid user, EntityUid target, float delay, DoAfterEvent doAfterEv)
    {
        var doAfterArgs = new DoAfterArgs(entManager, user, delay, doAfterEv, target, target: target, used: item)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        return entManager.System<SharedDoAfterSystem>().TryStartDoAfter(doAfterArgs);
    }

    public override void Commit(IEntityManager entManager, IPrototypeManager protoManager, EntityUid item)
    {
        Requirement.PostCraft(entManager, protoManager, new HashSet<EntityUid> { item });
    }

    public override string? GetExamineHint(IPrototypeManager protoManager)
    {
        return Requirement.GetRequirementTitle(protoManager);
    }
}
