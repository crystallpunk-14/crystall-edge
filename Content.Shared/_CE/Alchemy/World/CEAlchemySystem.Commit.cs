using Content.Shared._CE.Alchemy.Data;
using Content.Shared._CE.Alchemy.Effects;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Alchemy.World;

/// <summary>
/// Commit: turns raw reagents of a container into steps of the recipe of an alchemy mixture.
/// Raw reagents never become a mixture on their own: the container must already hold some mixture
/// (even a drop of the blank one that is sold in the shops), the raw reagents are absorbed into its recipe.
/// </summary>
public sealed partial class CEAlchemySystem
{
    public static readonly ProtoId<ReagentPrototype> MixtureReagent = "CEAlchemyMixture";
    public static readonly ProtoId<ReagentPrototype> SludgeReagent = "CEAlchemySludge";

    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private static readonly SoundSpecifier TransmuteSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");
    private static readonly EntProtoId TransmuteVfx = "CEAlchemyTransmuteVFX";

    /// <summary>
    /// Commits the raw reagents of the solution into the recipe of the mixture that is already there.
    /// <list type="bullet">
    /// <item>One mixture in the container: the raw reagents continue its recipe.</item>
    /// <item>Several mixtures: the blank one (no recipe yet) takes the raw reagents, finished ones are not touched.
    /// Without a blank one nothing happens.</item>
    /// <item>No mixture: nothing happens, raw reagents do not make a mixture by themselves.</item>
    /// <item>A recipe longer than <see cref="CEAlchemyData.MaxSteps"/> spoils everything involved into sludge.</item>
    /// </list>
    /// Carriers go into the recipe before the other reagents, inside groups by reagent id.
    /// </summary>
    public CEAlchemyCommitResult TryCommit(Entity<SolutionComponent> soln, EntityUid? user = null)
    {
        if (_timing.ApplyingState)
            return CEAlchemyCommitResult.Nothing;

        var contents = soln.Comp.Solution.Contents;
        var raws = new List<ReagentQuantity>();
        var mixtureIndices = new List<int>();
        var blankIndex = -1;
        for (var i = 0; i < contents.Count; i++)
        {
            var reagent = contents[i].Reagent;
            if (reagent.Prototype == SludgeReagent)
                continue;

            if (reagent.Prototype != MixtureReagent)
            {
                raws.Add(contents[i]);
                continue;
            }

            mixtureIndices.Add(i);
            if (GetData(reagent) == null)
                blankIndex = i;
        }

        if (raws.Count == 0)
            return CEAlchemyCommitResult.Nothing;

        if (mixtureIndices.Count == 0)
            return CEAlchemyCommitResult.NoMixture;

        int targetIndex;
        if (mixtureIndices.Count == 1)
            targetIndex = mixtureIndices[0];
        else if (blankIndex >= 0)
            targetIndex = blankIndex;
        else
            return CEAlchemyCommitResult.Ambiguous;

        raws.Sort(CompareRaw);

        var target = contents[targetIndex];
        var oldData = GetData(target.Reagent);
        var data = oldData?.Clone() as CEAlchemyData;
        if (data == null)
        {
            // The blank mixture is a plain solvent: it becomes the first step so it dilutes the ingredients.
            data = new CEAlchemyData();
            data.Steps.Add(new CEAlchemyMixStep { Reagent = MixtureReagent, Amount = target.Quantity });
        }

        var current = target.Quantity;
        var total = target.Quantity;
        foreach (var raw in raws)
        {
            data.AddStep(raw.Reagent.Prototype, raw.Quantity, current);
            current += raw.Quantity;
            total += raw.Quantity;
        }

        var kept = new List<ReagentQuantity>(contents.Count);
        for (var i = 0; i < contents.Count; i++)
        {
            if (i == targetIndex || raws.Contains(contents[i]))
                continue;

            kept.Add(contents[i]);
        }

        var spoiled = data.Steps.Count > CEAlchemyData.MaxSteps;
        soln.Comp.Solution.SetContents(kept);
        if (spoiled)
            soln.Comp.Solution.AddReagent(new ReagentId(SludgeReagent, null), total);
        else
            soln.Comp.Solution.AddReagent(new ReagentId(MixtureReagent, new List<ReagentData> { data }), total);

        _solution.UpdateChemicals(soln, false);
        if (spoiled)
            return CEAlchemyCommitResult.Spoiled;

        if (!ZonesChanged(oldData, data, out var lostEffects))
            return CEAlchemyCommitResult.Committed;

        PlayTransmute(_solution.GetSolutionOwner(soln), data, lostEffects, user);
        return CEAlchemyCommitResult.Transmuted;
    }

    /// <summary>
    /// Replaces an entry of the alchemy mixture with the same quantity but other data.
    /// For tuning devices that change <see cref="CEAlchemyData.Tune"/>.
    /// </summary>
    public bool TryReplaceData(Entity<SolutionComponent> soln, ReagentId mixture, CEAlchemyData newData)
    {
        if (_timing.ApplyingState || mixture.Prototype != MixtureReagent)
            return false;

        if (!soln.Comp.Solution.TryGetReagent(mixture, out var existing))
            return false;

        soln.Comp.Solution.RemoveReagent(existing);
        soln.Comp.Solution.AddReagent(new ReagentId(MixtureReagent, new List<ReagentData> { newData }), existing.Quantity);
        _solution.UpdateChemicals(soln, false);
        return true;
    }

    /// <summary>
    /// Commits the mixable solution of a vessel.
    /// </summary>
    public CEAlchemyCommitResult TryCommitVessel(EntityUid vessel, EntityUid? user)
    {
        if (!HasComp<CEAlchemyVesselComponent>(vessel))
            return CEAlchemyCommitResult.Nothing;

        if (!_solution.TryGetMixableSolution(vessel, out var soln, out _))
            return CEAlchemyCommitResult.Nothing;

        return TryCommit(soln.Value, user);
    }

    [SubscribeLocalEvent]
    private void OnVesselShake(Entity<CEAlchemyVesselComponent> ent, ref ShakeEvent args)
    {
        TryCommitVessel(ent, args.Shaker);
    }

    // The mixer event names are swapped upstream: the first field is the mixing item, the second one the mixed container.
    [SubscribeLocalEvent]
    private void OnMixerStirred(Entity<ReactionMixerComponent> ent, ref AfterMixingEvent args)
    {
        if (args.Mixer == ent.Owner)
            return;

        TryCommitVessel(args.Mixer, null);
    }

    /// <summary>
    /// True if the commit changed the zones of the mixture noticeably: it entered a zone (a target or a side effect)
    /// that the old recipe did not hit, or it lost every effect it had.
    /// </summary>
    private bool ZonesChanged(CEAlchemyData? oldData, CEAlchemyData newData, out bool lostEffects)
    {
        var known = new HashSet<string>();
        if (oldData != null)
        {
            foreach (var hit in Analyze(oldData))
            {
                known.Add(hit.Zone.ID);
            }
        }

        var hits = Analyze(newData);
        lostEffects = hits.Count == 0 && known.Count > 0;
        if (lostEffects)
            return true;

        foreach (var hit in hits)
        {
            if (!known.Contains(hit.Zone.ID))
                return true;
        }

        return false;
    }

    private void PlayTransmute(EntityUid vessel, CEAlchemyData data, bool lostEffects, EntityUid? user)
    {
        _audio.PlayPredicted(TransmuteSound, vessel, user);

        // A mixture without effects keeps the default color of the reagent
        var color = lostEffects ? null : data.GetColor(_proto);
        if (color == null && _proto.TryIndex(MixtureReagent, out var mixture))
            color = mixture.SubstanceColor;

        var vfx = PredictedSpawnAtPosition(TransmuteVfx, Transform(vessel).Coordinates);
        _appearance.SetData(vfx, CEAlchemyTransmuteVisuals.Color, color ?? Color.White);
    }

    private CEAlchemyData? GetData(ReagentId reagent)
    {
        if (reagent.Data == null)
            return null;

        foreach (var data in reagent.Data)
        {
            if (data is CEAlchemyData alchemy)
                return alchemy;
        }

        return null;
    }

    private int CompareRaw(ReagentQuantity a, ReagentQuantity b)
    {
        var carrierA = IsCarrier(a.Reagent.Prototype);
        var carrierB = IsCarrier(b.Reagent.Prototype);
        if (carrierA != carrierB)
            return carrierA ? -1 : 1;

        return string.CompareOrdinal(a.Reagent.Prototype, b.Reagent.Prototype);
    }

    private bool IsCarrier(ProtoId<ReagentPrototype> reagent)
    {
        if (!_proto.Resolve(reagent, out var proto))
            return false;

        foreach (var effect in proto.AlchemyEffects)
        {
            if (effect is CoordinatePoint)
                return true;
        }

        return false;
    }
}

public enum CEAlchemyCommitResult
{
    /// <summary>Nothing to commit or nothing changed.</summary>
    Nothing,

    /// <summary>There are raw reagents but no alchemy mixture to absorb them.</summary>
    NoMixture,

    /// <summary>Several finished mixtures and no blank one: unclear where the raw reagents go.</summary>
    Ambiguous,

    Committed,

    /// <summary>Committed and the mixture entered a new zone or lost all its effects: it plays the sound and the flash.</summary>
    Transmuted,

    /// <summary>The recipe grew too long, everything turned into sludge.</summary>
    Spoiled,
}
