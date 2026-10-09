using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.RestorationRitual.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Popups;

namespace Content.Shared._CE.RestorationRitual;

/// <summary>
/// Validates <see cref="CERestorationRitualActionComponent"/> targets before the action is used.
/// </summary>
public abstract partial class CESharedRestorationRitualSystem : EntitySystem
{
    [Dependency] protected SharedPopupSystem Popup = default!;

    [SubscribeLocalEvent]
    private void OnValidate(Entity<CERestorationRitualActionComponent> ent, ref ActionValidateEvent args)
    {
        if (args.Invalid)
            return;

        if (GetEntity(args.Input.EntityTarget) is not { Valid: true } target)
            return;

        if (GetSharedInvalidReason(target) is { } reason)
        {
            Popup.PopupClient(reason, args.User, args.User);
            args.Invalid = true;
            return;
        }

        if (GetServerInvalidReason(target) is { } serverReason)
        {
            Popup.PopupEntity(serverReason, args.User, args.User);
            args.Invalid = true;
        }
    }

    /// <summary>
    /// Checks the client can predict: the sphere's state.
    /// </summary>
    protected string? GetSharedInvalidReason(EntityUid target)
    {
        if (!TryComp<CEMurkLusconSphereComponent>(target, out var sphere))
            return Loc.GetString("ce-restoration-ritual-unavailable");

        return sphere.State switch
        {
            CEMurkSphereState.InGame => null,
            CEMurkSphereState.PreRound => Loc.GetString("ce-restoration-ritual-sphere-whole"),
            _ => Loc.GetString("ce-restoration-ritual-unavailable"),
        };
    }

    /// <summary>
    /// Checks only the server can make: the shards on the ritual pedestals.
    /// </summary>
    protected virtual string? GetServerInvalidReason(EntityUid target)
    {
        return null;
    }
}
