using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared.Mind;

namespace Content.Shared._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetHolderObjectiveComponent"/>.
/// </summary>
public sealed partial class CETargetHolderObjectiveSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnGetObjectiveTargetCandidates(Entity<CETargetHolderObjectiveComponent> ent, ref CEGetObjectiveTargetCandidatesEvent args)
    {
        if (TryComp<MindComponent>(args.Holder, out var mind) && mind.OwnedEntity is { } body)
            args.Candidates.Add(body);
    }
}
