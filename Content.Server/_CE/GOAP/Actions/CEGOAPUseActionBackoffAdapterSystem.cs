using Content.Server._CE.GOAP.Navigation;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>
/// Optionally excludes the target of an unhandled use-action attempt.
/// Execution success remains owned by <see cref="CEGOAPUseActionSystem"/>;
/// this adapter only applies the target-backoff policy when the agent opts in.
/// </summary>
public sealed partial class CEGOAPUseActionBackoffAdapterSystem : EntitySystem
{
    [Dependency] private CEGOAPTargetBackoffSystem _backoff = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEGOAPTargetBackoffComponent, CEGOAPUseActionTargetFailedEvent>(
            OnTargetFailed);
    }

    private void OnTargetFailed(
        Entity<CEGOAPTargetBackoffComponent> ent,
        ref CEGOAPUseActionTargetFailedEvent args)
    {
        if (args.Selector is ICEGOAPTargetBackoffSelector)
            _backoff.Reject(ent.Owner, args.Target);
    }
}
