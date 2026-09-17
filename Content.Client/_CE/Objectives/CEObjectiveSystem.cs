using Content.Shared._CE.Objectives;
using Content.Shared._CE.Objectives.Components;
using Robust.Shared.GameStates;

namespace Content.Client._CE.Objectives;

/// <inheritdoc/>
public sealed partial class CEObjectiveSystem : CESharedObjectiveSystem
{
    public event Action<EntityUid>? OnObjectivesChanged;
    public event Action<Entity<CEObjectiveComponent>>? OnObjectiveProgressChanged;

    [SubscribeLocalEvent]
    private void OnHolderAfterAutoHandleState(Entity<CEObjectiveHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OnObjectivesChanged?.Invoke(ent);
    }

    [SubscribeLocalEvent]
    private void OnObjectiveAfterAutoHandleState(Entity<CEObjectiveComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OnObjectiveProgressChanged?.Invoke(ent);
    }

    [SubscribeLocalEvent]
    private void OnDescriptorAfterAutoHandleState(Entity<CEObjectiveDescriptorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp<CEObjectiveComponent>(ent.Owner, out var objective))
            OnObjectiveProgressChanged?.Invoke((ent.Owner, objective));
    }
}
