using Content.Server._CE.Power.Components;
using Content.Server.Administration.Logs;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Shared._CE.EntityEffect;
using Content.Shared.Construction;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.NodeContainer;
using Content.Shared.Tools.Systems;

namespace Content.Server._CE.Power;

/// <summary>
/// Makes powered pipes dangerous to tamper with, see <see cref="CEEnergizedInteractionComponent"/>.
/// Construction doAfters cover wrenching and installing parts (e.g. valves), cable cutting covers prying.
/// </summary>
public sealed partial class CEEnergizedInteractionSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private EntityQuery<NodeContainerComponent> _nodeQuery = default!;
    [Dependency] private EntityQuery<ApcPowerReceiverComponent> _receiverQuery = default!;

    [SubscribeLocalEvent]
    private void OnDamageChanged(Entity<CEEnergizedInteractionComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta is null)
            return;

        if (!IsPowered(ent))
            return;

        Discharge(ent);
    }

    [SubscribeLocalEvent(before: [typeof(CableSystem)])]
    private void OnCableCut(Entity<CEEnergizedInteractionComponent> ent, ref CableCuttingFinishedEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryInteract(ent, args.User, "pry"))
            args.Handled = true;
    }

    [SubscribeLocalEvent(before: [typeof(ConstructionSystem)])]
    private void OnConstructionDoAfter(Entity<CEEnergizedInteractionComponent> ent, ref ConstructionInteractDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryInteract(ent, args.User, "reconstruct"))
            args.Handled = true;
    }

    [SubscribeLocalEvent(before: [typeof(AnchorableSystem)])]
    private void OnUnanchorDoAfter(Entity<CEEnergizedInteractionComponent> ent, ref AnchorableSystem.TryUnanchorCompletedEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryInteract(ent, args.User, "unanchor"))
            args.Handled = true;
    }

    /// <summary>
    /// Discharges a powered entity and checks whether the user is allowed to finish the interaction.
    /// Unpowered entities always allow it.
    /// </summary>
    private bool TryInteract(Entity<CEEnergizedInteractionComponent> ent, EntityUid user, string action)
    {
        if (!IsPowered(ent))
            return true;

        var userArgs = new CEEntityEffectArgs(EntityManager, ent, null, Angle.Zero, 0f, user, null);
        var allowed = true;
        foreach (var condition in ent.Comp.UserConditions)
        {
            if (condition.Passes(userArgs))
                continue;

            allowed = false;
            break;
        }

        Discharge(ent);

        if (!allowed)
        {
            _adminLog.Add(LogType.Electrocution,
                LogImpact.Medium,
                $"{ToPrettyString(user):user} was shocked trying to {action} energized {ToPrettyString(ent):target}");
        }

        return allowed;
    }

    private void Discharge(Entity<CEEnergizedInteractionComponent> ent)
    {
        var effectArgs = new CEEntityEffectArgs(EntityManager,
            ent,
            null,
            Angle.Zero,
            0f,
            null,
            Transform(ent).Coordinates);

        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(effectArgs);
        }
    }

    /// <summary>
    /// Powered APC receivers (machines) or anything whose own power net has supply (pipes, batteries).
    /// </summary>
    private bool IsPowered(EntityUid uid)
    {
        if (_receiverQuery.TryComp(uid, out var receiver) && receiver.Powered)
            return true;

        if (!_nodeQuery.TryComp(uid, out var nodeContainer))
            return false;

        foreach (var node in nodeContainer.Nodes.Values)
        {
            if (node.NodeGroup is IBasePowerNet net && net.NetworkNode.LastCombinedMaxSupply > 0)
                return true;
        }

        return false;
    }
}
