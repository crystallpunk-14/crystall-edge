using Content.Shared._CE.Alchemy.Data;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Alchemy.Tuner;

public sealed partial class CEAlchemyTunerSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly FixedPoint2 TuneLimit = FixedPoint2.New(1);

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEAlchemyTunerComponent> ent, ref MapInitEvent args)
    {
        Reevaluate(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CEAlchemyTunerComponent> ent, ref PowerChangedEvent args)
    {
        Reevaluate(ent);
    }

    [SubscribeLocalEvent]
    private void OnSolutionChanged(Entity<CEAlchemyTunerComponent> ent, ref SolutionChangedEvent args)
    {
        if (args.Solution.Comp.Id != ent.Comp.SolutionName)
            return;

        Reevaluate(ent);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<CEAlchemyTunerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ce-alchemy-tuner-examine", ("mode", Loc.GetString(GetModeName(ent.Comp.Mode)))));
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<CEAlchemyTunerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        AddModeVerb(ent, ref args, CEAlchemyTunerMode.Concentrate, user, 1);
        AddModeVerb(ent, ref args, CEAlchemyTunerMode.Extend, user, 0);
    }

    private void AddModeVerb(
        Entity<CEAlchemyTunerComponent> ent,
        ref GetVerbsEvent<InteractionVerb> args,
        CEAlchemyTunerMode mode,
        EntityUid user,
        int priority)
    {
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString(GetModeName(mode)),
            Message = Loc.GetString(GetModeDescription(mode)),
            Disabled = ent.Comp.Mode == mode,
            Priority = priority,
            Act = () => SetMode(ent, mode, user),
        });
    }

    private void SetMode(Entity<CEAlchemyTunerComponent> ent, CEAlchemyTunerMode mode, EntityUid user)
    {
        if (ent.Comp.Mode == mode)
            return;

        ent.Comp.Mode = mode;
        Dirty(ent);
        _audio.PlayPredicted(ent.Comp.ClickSound, ent, user);

        // The direction changed: the running cycle is dropped and the next one goes the other way
        Stop(ent);
        Reevaluate(ent);
    }

    [SubscribeLocalEvent]
    private void OnCycleFinished(Entity<CEAlchemyTunerComponent> ent, ref CEAlchemyTunerDoAfter args)
    {
        ent.Comp.DoAfterId = null;

        if (args.Cancelled || args.Handled)
        {
            Stop(ent);
            return;
        }

        args.Handled = true;

        if (_timing.ApplyingState)
            return;

        if (_solution.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution)
            && ent.Comp.Solution is { } soln)
        {
            ApplyStep(ent, soln, solution);
        }

        // The next cycle starts if there is anything left to tune
        Stop(ent);
        Reevaluate(ent);
    }

    private void ApplyStep(Entity<CEAlchemyTunerComponent> ent, Entity<SolutionComponent> soln, Solution solution)
    {
        var delta = ent.Comp.Mode == CEAlchemyTunerMode.Concentrate ? ent.Comp.Step : -ent.Comp.Step;
        var changes = new List<(ReagentQuantity Old, ReagentId New)>();

        foreach (var quantity in solution.Contents)
        {
            if (quantity.Reagent.Data is not { } dataList)
                continue;

            var newList = new List<ReagentData>(dataList.Count);
            var changed = false;
            foreach (var data in dataList)
            {
                if (data is CEAlchemyData alchemy && CanMove(alchemy.Tune, ent.Comp.Mode))
                {
                    var tuned = (CEAlchemyData) alchemy.Clone();
                    tuned.Tune = Clamp(alchemy.Tune + delta);
                    newList.Add(tuned);
                    changed = true;
                    continue;
                }

                newList.Add(data.Clone());
            }

            if (changed)
                changes.Add((quantity, new ReagentId(quantity.Reagent.Prototype, newList)));
        }

        if (changes.Count == 0)
            return;

        foreach (var (old, tuned) in changes)
        {
            solution.RemoveReagent(old);
            solution.AddReagent(tuned, old.Quantity);
        }

        _solution.UpdateChemicals(soln, false);
    }

    /// <summary>
    /// Starts the cycle if the device has something to do, stops it otherwise.
    /// </summary>
    private void Reevaluate(Entity<CEAlchemyTunerComponent> ent)
    {
        if (_net.IsClient)
            return;

        if (ShouldWork(ent))
            Start(ent);
        else
            Stop(ent);
    }

    private bool ShouldWork(Entity<CEAlchemyTunerComponent> ent)
    {
        if (!_power.IsPowered(ent.Owner))
            return false;

        if (!_solution.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return false;

        foreach (var quantity in solution.Contents)
        {
            if (quantity.Reagent.Data is not { } dataList)
                continue;

            foreach (var data in dataList)
            {
                if (data is CEAlchemyData alchemy && CanMove(alchemy.Tune, ent.Comp.Mode))
                    return true;
            }
        }

        return false;
    }

    private void Start(Entity<CEAlchemyTunerComponent> ent)
    {
        if (_doAfter.IsRunning(ent.Comp.DoAfterId))
            return;

        var args = new DoAfterArgs(EntityManager, ent, ent.Comp.CycleTime, new CEAlchemyTunerDoAfter(), ent)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            RequireCanInteract = false,
        };

        _doAfter.TryStartDoAfter(args, out var id);
        ent.Comp.DoAfterId = id;

        _ambient.SetAmbience(ent, true);
    }

    private void Stop(Entity<CEAlchemyTunerComponent> ent)
    {
        if (_doAfter.IsRunning(ent.Comp.DoAfterId))
            _doAfter.Cancel(ent.Comp.DoAfterId);

        ent.Comp.DoAfterId = null;

        _ambient.SetAmbience(ent, false);
    }

    private static bool CanMove(FixedPoint2 tune, CEAlchemyTunerMode mode)
    {
        return mode == CEAlchemyTunerMode.Concentrate ? tune < TuneLimit : tune > -TuneLimit;
    }

    private static FixedPoint2 Clamp(FixedPoint2 tune)
    {
        if (tune > TuneLimit)
            return TuneLimit;

        return tune < -TuneLimit ? -TuneLimit : tune;
    }

    private static string GetModeName(CEAlchemyTunerMode mode)
    {
        return mode == CEAlchemyTunerMode.Concentrate
            ? "ce-alchemy-tuner-mode-concentrate"
            : "ce-alchemy-tuner-mode-extend";
    }

    private static string GetModeDescription(CEAlchemyTunerMode mode)
    {
        return mode == CEAlchemyTunerMode.Concentrate
            ? "ce-alchemy-tuner-mode-concentrate-desc"
            : "ce-alchemy-tuner-mode-extend-desc";
    }
}
