using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server._CE.MagicEssence;

public sealed partial class CEMagicEssenceAttractorSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEMagicEssenceAttractorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CEMagicEssenceAttractorComponent, StartCollideEvent>(OnCollide);
    }

    private void OnPowerChanged(Entity<CEMagicEssenceAttractorComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            EnsureComp<CEMagicEssenceAttractingComponent>(ent);
            return;
        }

        RemCompDeferred<CEMagicEssenceAttractingComponent>(ent);
    }

    private void OnCollide(Entity<CEMagicEssenceAttractorComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<CEMagicEssenceAttractingComponent>(ent))
            return;

        if (!_solutionContainer.TryGetSolution((args.OtherEntity, null), "essence", out _, out var essenceSolution))
            return;

        if (!_solutionContainer.TryGetSolution((ent.Owner, null), ent.Comp.Solution, out var collectorSoln, out _))
            return;

        if (!_solutionContainer.TryTransferSolution(collectorSoln.Value, essenceSolution, essenceSolution.Volume))
            return;

        _audio.PlayPvs(ent.Comp.ConsumeSound, ent);

        QueueDel(args.OtherEntity);
    }
}