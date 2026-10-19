using System.Numerics;
using Content.Server._CE.Murk.Components;
using Content.Shared._CE.GOAP.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.Murk;

/// <summary>
/// Keeps track of two-sided conversations: who holds the turn, when it passes to the other side
/// and when the conversation falls apart. Speaking itself is done by the participants.
/// </summary>
public sealed partial class CEMurkConversationSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [Dependency] private EntityQuery<CEMurkConversationComponent> _conversationQuery = default!;
    [Dependency] private EntityQuery<CEActiveGOAPComponent> _activeGoapQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    private readonly List<EntityUid> _ended = new();
    private readonly List<Entity<CEMurkConversationComponent>> _turns = new();

    /// <summary>
    /// Starts a conversation of <paramref name="lines"/> lines, counting the initiator's opening line,
    /// which the initiator is expected to say right away.
    /// </summary>
    public bool TryStart(
        EntityUid initiator,
        EntityUid partner,
        int lines,
        TimeSpan minTurnDelay,
        TimeSpan maxTurnDelay,
        TimeSpan turnTimeout,
        float maxDistance)
    {
        if (initiator == partner || _conversationQuery.HasComp(initiator) || _conversationQuery.HasComp(partner))
            return false;

        foreach (var (self, other) in new[] { (initiator, partner), (partner, initiator) })
        {
            var comp = AddComp<CEMurkConversationComponent>(self);
            comp.Partner = other;
            comp.LinesLeft = lines;
            comp.MinTurnDelay = minTurnDelay;
            comp.MaxTurnDelay = maxTurnDelay;
            comp.TurnTimeout = turnTimeout;
            comp.MaxDistance = maxDistance;
        }

        PassTurn(initiator);
        return true;
    }

    /// <summary>
    /// Counts the line just said by <paramref name="speaker"/> and hands the turn over: to the partner if it can
    /// answer, otherwise back to the speaker. Ends the conversation when no lines are left.
    /// </summary>
    public void PassTurn(EntityUid speaker)
    {
        if (!_conversationQuery.TryComp(speaker, out var comp) ||
            !_conversationQuery.TryComp(comp.Partner, out var partnerComp))
        {
            End(speaker);
            return;
        }

        RemComp<CEMurkConversationTurnComponent>(speaker);
        comp.LinesLeft--;
        partnerComp.LinesLeft = comp.LinesLeft;

        if (comp.LinesLeft <= 0)
        {
            End(speaker);
            return;
        }

        var next = CanAnswer(comp.Partner) ? (comp.Partner, partnerComp) : (speaker, comp);
        var turnAt = _timing.CurTime + _random.Next(comp.MinTurnDelay, comp.MaxTurnDelay);

        comp.TurnAt = null;
        partnerComp.TurnAt = null;
        next.Item2.TurnAt = turnAt;

        comp.Deadline = turnAt + comp.TurnTimeout;
        partnerComp.Deadline = comp.Deadline;
    }

    /// <summary>
    /// Ends the conversation of <paramref name="uid"/> for both sides.
    /// </summary>
    public void End(EntityUid uid)
    {
        if (_conversationQuery.TryComp(uid, out var comp) &&
            _conversationQuery.TryComp(comp.Partner, out var partnerComp) &&
            partnerComp.Partner == uid)
        {
            RemComp<CEMurkConversationComponent>(comp.Partner);
            RemComp<CEMurkConversationTurnComponent>(comp.Partner);
        }

        RemComp<CEMurkConversationComponent>(uid);
        RemComp<CEMurkConversationTurnComponent>(uid);
    }

    /// <summary>
    /// Turns <paramref name="uid"/> to face its conversation partner.
    /// </summary>
    public void FacePartner(EntityUid uid)
    {
        if (_conversationQuery.TryComp(uid, out var comp))
            FaceTo(uid, comp.Partner);
    }

    public void FaceTo(EntityUid uid, EntityUid target)
    {
        if (!_xformQuery.TryComp(uid, out var xform) || !_xformQuery.TryComp(target, out var targetXform))
            return;

        var dir = _transform.GetWorldPosition(targetXform) - _transform.GetWorldPosition(xform);
        if (dir == Vector2.Zero)
            return;

        _transform.SetLocalRotationNoLerp(uid, dir.ToWorldAngle(), xform);
    }

    private bool CanAnswer(EntityUid uid)
    {
        return _activeGoapQuery.HasComp(uid);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<CEMurkConversationComponent> ent, ref PlayerAttachedEvent args)
    {
        End(ent);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;

        _ended.Clear();
        _turns.Clear();

        var query = EntityQueryEnumerator<CEMurkConversationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (ShouldEnd((uid, comp), now))
            {
                _ended.Add(uid);
                continue;
            }

            if (comp.TurnAt is { } turnAt && now >= turnAt)
                _turns.Add((uid, comp));
        }

        foreach (var uid in _ended)
        {
            End(uid);
        }

        foreach (var (uid, comp) in _turns)
        {
            if (TerminatingOrDeleted(uid) || !_conversationQuery.HasComp(uid))
                continue;

            comp.TurnAt = null;
            EnsureComp<CEMurkConversationTurnComponent>(uid);
        }
    }

    private bool ShouldEnd(Entity<CEMurkConversationComponent> ent, TimeSpan now)
    {
        if (now > ent.Comp.Deadline)
            return true;

        var partner = ent.Comp.Partner;
        if (TerminatingOrDeleted(partner) ||
            !_conversationQuery.TryComp(partner, out var partnerComp) ||
            partnerComp.Partner != ent.Owner)
        {
            return true;
        }

        if (_mobState.IsIncapacitated(ent) || _mobState.IsIncapacitated(partner))
            return true;

        if (!_xformQuery.TryComp(ent, out var xform) || !_xformQuery.TryComp(partner, out var partnerXform))
            return true;

        var pos = _transform.GetMapCoordinates(xform);
        var partnerPos = _transform.GetMapCoordinates(partnerXform);
        return pos.MapId != partnerPos.MapId ||
               Vector2.Distance(pos.Position, partnerPos.Position) > ent.Comp.MaxDistance;
    }
}
