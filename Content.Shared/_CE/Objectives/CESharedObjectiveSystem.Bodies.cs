using Content.Shared._CE.Character;

namespace Content.Shared._CE.Objectives;

public abstract partial class CESharedObjectiveSystem
{
    [Dependency] private CECharacterSystem _character = default!;

    /// <summary>
    /// Whether protecting the body counts as done - see <see cref="CECharacterSystem.IsSurvived"/>.
    /// </summary>
    public bool IsBodySurvived(EntityUid body)
    {
        return CECharacterSystem.IsSurvived(_character.GetBodyState(body));
    }

    /// <summary>
    /// Progress of an objective keeping the body alive: alive or left the round = 1, critical = 0.5, otherwise 0.
    /// </summary>
    public float GetSurvivalProgress(EntityUid body)
    {
        return _character.GetBodyState(body) switch
        {
            CEBodyState.Alive or CEBodyState.LeftRound => 1f,
            CEBodyState.Critical => 0.5f,
            _ => 0f,
        };
    }

    /// <summary>
    /// Progress of an objective getting rid of the body: eliminated = 1, critical = 0.5, otherwise 0.
    /// See <see cref="CECharacterSystem.IsEliminated"/>.
    /// </summary>
    public float GetEliminationProgress(EntityUid body)
    {
        var state = _character.GetBodyState(body);
        if (CECharacterSystem.IsEliminated(state))
            return 1f;

        return state == CEBodyState.Critical ? 0.5f : 0f;
    }
}
