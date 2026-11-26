using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Alchemy;

/// <summary>
/// Uses <see cref="CEAlchemyInjectorComponent"/>: pours the whole solution of the injector into the bloodstream of the target.
/// </summary>
public sealed partial class CEAlchemyInjectorSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<CEAlchemyInjectorComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStart(ent, args.User, args.User);
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<CEAlchemyInjectorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true } target)
            return;

        args.Handled = TryStart(ent, args.User, target);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<CEAlchemyInjectorComponent> ent, ref CEAlchemyInjectorDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        args.Handled = true;
        Inject(ent, args.User, target);
    }

    private bool TryStart(Entity<CEAlchemyInjectorComponent> ent, EntityUid user, EntityUid target)
    {
        if (!HasComp<BloodstreamComponent>(target))
            return false;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out _, out var solution) || solution.Volume <= 0)
        {
            _popup.PopupClient(Loc.GetString("injector-component-empty-message", ("injector", ent.Owner)), user, user);
            return true;
        }

        var self = user == target;
        if (!self && !_interaction.InRangeUnobstructed(user, target, popup: true))
            return true;

        _audio.PlayPredicted(ent.Comp.BeginSound, ent, user);

        if (!self)
        {
            var msg = Loc.GetString("injector-component-needle-injecting-target", ("user", Identity.Entity(user, EntityManager)));
            _popup.PopupEntity(msg, target, target, PopupType.MediumCaution);
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            self ? ent.Comp.SelfDelay : ent.Comp.OtherDelay,
            new CEAlchemyInjectorDoAfterEvent(),
            ent,
            target: target,
            used: ent)
        {
            NeedHand = true,
            BreakOnMove = self ? ent.Comp.BreakOnMoveSelf : ent.Comp.BreakOnMoveOther,
            BreakOnWeightlessMove = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        return true;
    }

    private void Inject(Entity<CEAlchemyInjectorComponent> ent, EntityUid user, EntityUid target)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var source, out var sourceSolution) || sourceSolution.Volume <= 0)
            return;

        if (!_solution.TryGetInjectableSolution(target, out var bloodstream, out _))
            return;

        var color = sourceSolution.GetColor(_proto);
        var injected = _solution.SplitSolution(source.Value, sourceSolution.Volume);

        _solution.Inject(target, bloodstream.Value, injected);

        _adminLog.Add(LogType.ForceFeed,
            $"{ToPrettyString(user):user} injected {ToPrettyString(target):target} with {SharedSolutionContainerSystem.ToPrettyString(injected):solution} using {ToPrettyString(ent):used}");

        _audio.PlayPredicted(ent.Comp.InjectSound, target, user);

        if (ent.Comp.Vfx is { } vfxProto)
        {
            var vfx = PredictedSpawnAtPosition(vfxProto, Transform(target).Coordinates);
            _appearance.SetData(vfx, CEAlchemyTransmuteVisuals.Color, color);
        }

        _popup.PopupClient(Loc.GetString(target == user ? "injector-component-inject-success-message-self" : "injector-component-inject-success-message", ("amount", injected.Volume), ("target", Identity.Entity(target, EntityManager))), target, user);

        if (target != user)
            _popup.PopupEntity(Loc.GetString("injector-component-feel-prick-message"), target, target);

    }
}

[Serializable, NetSerializable]
public sealed partial class CEAlchemyInjectorDoAfterEvent : SimpleDoAfterEvent;
