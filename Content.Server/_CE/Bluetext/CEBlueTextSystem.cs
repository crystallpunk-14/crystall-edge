using Content.Server.Antag;
using Content.Server.Mind;

namespace Content.Server._CE.Bluetext;

public sealed class CEBlueTextSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEBlueTextRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagAttached);
    }

    private void OnAntagAttached(Entity<CEBlueTextRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.Session, out var mind, out var mindComp))
            return;

        EnsureComp<CEBlueTextTrackerComponent>(mind);
    }
}
