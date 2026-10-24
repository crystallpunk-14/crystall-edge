using Content.Shared._CE.Murk.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared._CE.Objectives;

public abstract partial class CESharedObjectiveSystem
{
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;
    [Dependency] private EntityQuery<CEMurkDissolvingComponent> _murkDissolvingQuery = default!;

    /// <summary>
    /// Whether a body counts as dead for objectives: deleted, dead, or dissolved into a murked soul.
    /// A soul is kept alive only so its AI can walk, there is no way back for the player.
    /// </summary>
    public bool IsBodyDead(EntityUid body)
    {
        if (TerminatingOrDeleted(body))
            return true;

        if (_murkDissolvingQuery.TryComp(body, out var dissolving) && dissolving.Converted)
            return true;

        return _mobStateQuery.TryComp(body, out var mobState) && mobState.CurrentState == MobState.Dead;
    }

    /// <summary>
    /// Whether a body that isn't <see cref="IsBodyDead"/> is currently in critical condition.
    /// </summary>
    public bool IsBodyCritical(EntityUid body)
    {
        return !IsBodyDead(body) &&
               _mobStateQuery.TryComp(body, out var mobState) &&
               mobState.CurrentState == MobState.Critical;
    }
}
