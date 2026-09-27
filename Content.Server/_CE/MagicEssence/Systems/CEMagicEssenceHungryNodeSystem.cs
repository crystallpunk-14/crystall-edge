using Content.Server._CE.MagicEssence.Components;
using Content.Shared._CE.MagicEssence.Components;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server._CE.MagicEssence.Systems;

public sealed partial class CEMagicEssenceHungryNodeSystem : EntitySystem
{
    [Dependency] private CEMagicEssenceSystem _essence = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CEMagicEssenceHungryNodeComponent> ent, ref MapInitEvent args)
    {
        var essenceA = _essence.GetRandomEssenceType();
        var essenceB = _essence.GetRandomEssenceType();
        var essenceC = _essence.GetRandomEssenceType();

        CESharedScienceSystem.DistributeInterestPoints(_random, ent.Comp.RequiredEssence,
            new MinMax(ent.Comp.HungerVolume, ent.Comp.HungerVolume), essenceA, essenceB, essenceC);

        ent.Comp.InitialRequiredEssence = new(ent.Comp.RequiredEssence);

        Dirty(ent);
    }

    /// <summary>
    /// Matching essence reduces the matching requirement; anything else (wrong type, or already
    /// satisfied) is just wasted. The orb is consumed either way. Once every requirement hits 0, the
    /// node is satisfied - see <see cref="Satisfy"/>.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCollide(Entity<CEMagicEssenceHungryNodeComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.Satisfied || !HasComp<CEFloatingEssenceComponent>(args.OtherEntity))
            return;

        if (!_solutionContainer.TryGetSolution((args.OtherEntity, null), "essence", out var essenceSoln, out _))
            return;

        foreach (var (type, amount) in _essence.GetEssence(args.OtherEntity, recursive: false))
        {
            if (!ent.Comp.RequiredEssence.TryGetValue(type, out var remaining))
                continue;

            if (amount >= remaining)
                ent.Comp.RequiredEssence.Remove(type);
            else
                ent.Comp.RequiredEssence[type] = remaining - amount;
        }

        Dirty(ent);
        _solutionContainer.RemoveAllSolution(essenceSoln.Value);
        _audio.PlayPvs(ent.Comp.ConsumeSound, ent);

        if (ent.Comp.RequiredEssence.Count == 0)
        {
            ent.Comp.Satisfied = true;
            Satisfy(ent);
        }
    }

    private void Satisfy(Entity<CEMagicEssenceHungryNodeComponent> ent)
    {
        var coordinates = Transform(ent).Coordinates;

        foreach (var vfx in ent.Comp.VFX)
        {
            Spawn(vfx, coordinates);
        }

        if (ent.Comp.Reward is { } rewardProto)
        {
            var reward = Spawn(rewardProto, coordinates);
            var interest = EnsureComp<CEScientificInterestComponent>(reward);

            foreach (var (type, amount) in ent.Comp.InitialRequiredEssence)
            {
                interest.Points[type] = amount * 2;
            }

            Dirty(reward, interest);
        }

        QueueDel(ent.Owner);
    }
}
