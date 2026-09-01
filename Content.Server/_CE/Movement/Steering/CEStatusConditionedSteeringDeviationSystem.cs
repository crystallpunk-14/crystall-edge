using System.Numerics;
using Content.Server.NPC.Events;
using Content.Server.NPC.Systems;
using Content.Shared.NPC;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Movement.Steering;

/// <summary>
/// Alters only the final NPC steering request. Goal selection, paths, fleeing and collision
/// avoidance remain owned by their existing systems.
/// </summary>
public sealed partial class CEStatusConditionedSteeringDeviationSystem : EntitySystem
{
    private const double MinimumPeriodSeconds = 0.1;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEStatusConditionedSteeringDeviationComponent, NPCSteeringEvent>(
            OnSteering,
            after: new[] { typeof(NPCJukeSystem) });
    }

    private void OnSteering(
        Entity<CEStatusConditionedSteeringDeviationComponent> entity,
        ref NPCSteeringEvent args)
    {
        var behavior = entity.Comp;
        if (!IsRequiredStatusActive(entity.Owner, behavior))
        {
            ResetDeviation(behavior);
            return;
        }

        var now = _timing.CurTime;
        if (behavior.NextDeviation == TimeSpan.Zero)
            ScheduleNextDeviation(behavior);

        if (behavior.DeviationEnd != TimeSpan.Zero && now >= behavior.DeviationEnd)
        {
            behavior.DeviationEnd = TimeSpan.Zero;
            behavior.DeviationDirection = Vector2.Zero;
        }

        if (behavior.DeviationEnd == TimeSpan.Zero)
        {
            if (now < behavior.NextDeviation || args.Steering.LastSteerDirection == Vector2.Zero)
                return;

            behavior.DeviationDirection = PickDifferentDirection(args.Steering.LastSteerDirection);
            behavior.DeviationEnd = now + RandomPeriod(
                behavior.MinDeviationDuration,
                behavior.MaxDeviationDuration);
            behavior.NextDeviation = behavior.DeviationEnd + RandomPeriod(
                behavior.MinDeviationInterval,
                behavior.MaxDeviationInterval);
        }

        var retainedInterest = Math.Clamp(behavior.RetainedInterest, 0f, 1f);
        for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
            args.Steering.Interest[i] *= retainedInterest;

        var deviationIndex = ClosestDirectionIndex(behavior.DeviationDirection);
        args.Steering.Interest[deviationIndex] = MathF.Max(args.Steering.Interest[deviationIndex], 1f);
        args.Steering.CanSeek = false;
    }

    private bool IsRequiredStatusActive(
        EntityUid uid,
        CEStatusConditionedSteeringDeviationComponent behavior)
    {
        if (!_statusEffects.HasStatusEffect(uid, behavior.RequiredStatusEffect) ||
            !_statusEffects.TryGetTime(uid, behavior.RequiredStatusEffect, out var time))
        {
            return false;
        }

        var now = _timing.CurTime;
        return (time.StartEffectTime == null || now >= time.StartEffectTime.Value) &&
            (time.EndEffectTime == null || now < time.EndEffectTime.Value);
    }

    private Vector2 PickDifferentDirection(Vector2 currentDirection)
    {
        var currentIndex = ClosestDirectionIndex(currentDirection);
        var offset = _random.Next(2, SharedNPCSteeringSystem.InterestDirections - 1);
        return NPCSteeringSystem.Directions[
            (currentIndex + offset) % SharedNPCSteeringSystem.InterestDirections];
    }

    private static int ClosestDirectionIndex(Vector2 direction)
    {
        var bestIndex = 0;
        var bestDot = float.NegativeInfinity;
        for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
        {
            var dot = Vector2.Dot(direction, NPCSteeringSystem.Directions[i]);
            if (dot <= bestDot)
                continue;

            bestDot = dot;
            bestIndex = i;
        }

        return bestIndex;
    }

    private void ScheduleNextDeviation(CEStatusConditionedSteeringDeviationComponent behavior)
    {
        behavior.DeviationEnd = TimeSpan.Zero;
        behavior.DeviationDirection = Vector2.Zero;
        behavior.NextDeviation = _timing.CurTime + RandomPeriod(
            behavior.MinDeviationInterval,
            behavior.MaxDeviationInterval);
    }

    private TimeSpan RandomPeriod(TimeSpan minimum, TimeSpan maximum)
    {
        var minSeconds = Math.Max(MinimumPeriodSeconds, minimum.TotalSeconds);
        var maxSeconds = Math.Max(minSeconds, maximum.TotalSeconds);
        if (Math.Abs(maxSeconds - minSeconds) < double.Epsilon)
            return TimeSpan.FromSeconds(minSeconds);

        return TimeSpan.FromSeconds(_random.NextDouble(minSeconds, maxSeconds));
    }

    private static void ResetDeviation(CEStatusConditionedSteeringDeviationComponent behavior)
    {
        behavior.NextDeviation = TimeSpan.Zero;
        behavior.DeviationEnd = TimeSpan.Zero;
        behavior.DeviationDirection = Vector2.Zero;
    }
}
