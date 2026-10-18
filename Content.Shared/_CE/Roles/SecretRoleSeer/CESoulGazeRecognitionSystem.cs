using Content.Shared.Mind.Components;

namespace Content.Shared._CE.Roles.SecretRoleSeer;

public sealed partial class CESoulGazeRecognitionSystem : EntitySystem
{
    [Dependency] private EntityQuery<MindContainerComponent> _mindContainerQuery = default!;

    [SubscribeLocalEvent]
    private void OnRecognize(Entity<CESoulGazeMemoryComponent> ent, ref CESecretRoleRecognizeEvent args)
    {
        if (_mindContainerQuery.TryComp(args.Target, out var mindContainer) &&
            mindContainer.Mind is { } targetMind &&
            ent.Comp.Minds.Contains(targetMind))
        {
            args.Recognized = true;
        }
    }
}
