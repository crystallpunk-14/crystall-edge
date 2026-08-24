using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Server._CE.Chemistry;

public sealed partial class CEDeleteOnEmptySolutionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEDeleteOnEmptySolutionComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(Entity<CEDeleteOnEmptySolutionComponent> ent, ref SolutionChangedEvent args)
    {
        if (args.Solution.Comp.Id != ent.Comp.SolutionId)
            return;

        if (args.Solution.Comp.Solution.Volume > FixedPoint2.Zero)
            return;

        QueueDel(ent.Owner);
    }
}
