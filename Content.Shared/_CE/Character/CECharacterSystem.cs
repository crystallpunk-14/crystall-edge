using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.Murk;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.RoundPortal;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SSDIndicator;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Character;

/// <summary>
/// The single source of truth for what happened to a player's character: which body it is, and whether
/// that body is alive, dead, dissolved, gone or left the round. Objectives, statistics, achievements and
/// the ghost warp menu all read it from here.
/// </summary>
public sealed partial class CECharacterSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;

    [Dependency] private EntityQuery<CECharacterComponent> _characterQuery = default!;
    [Dependency] private EntityQuery<CECharacterBodyComponent> _bodyQuery = default!;
    [Dependency] private EntityQuery<CERoundLeavedComponent> _leftQuery = default!;
    [Dependency] private EntityQuery<CEMurkDissolvingComponent> _murkQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;
    [Dependency] private EntityQuery<SSDIndicatorComponent> _ssdQuery = default!;

    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!_mind.TryGetMind(ev.Mob, out var mindId, out _))
            return;

        var character = EnsureComp<CECharacterComponent>(mindId);
        character.Job = ev.JobId;
        SetBody((mindId, character), ev.Mob);
    }

    // A dissolved body may not change mob state at all, so nothing else would announce it.
    [SubscribeLocalEvent]
    private void OnSoulConverted(ref CEMurkSoulConvertedEvent args)
    {
        RaiseBodyStateChanged(args.Body);
    }

    /// <summary>
    /// Moves a character into <paramref name="body"/>. A body belongs to one character only, so
    /// whoever owned it before is left without one.
    /// </summary>
    public void SetBody(Entity<CECharacterComponent> character, EntityUid? body)
    {
        if (character.Comp.Body is { } oldBody &&
            _bodyQuery.TryComp(oldBody, out var oldMarker) &&
            oldMarker.Mind == character.Owner)
        {
            RemComp<CECharacterBodyComponent>(oldBody);
        }

        character.Comp.Body = body;

        if (body is not { } newBody)
            return;

        if (_bodyQuery.TryComp(newBody, out var marker) &&
            marker.Mind != character.Owner &&
            _characterQuery.TryComp(marker.Mind, out var previous))
        {
            previous.Body = null;
        }

        EnsureComp<CECharacterBodyComponent>(newBody).Mind = character.Owner;
    }

    /// <summary>
    /// Moves the character of <paramref name="oldBody"/>, if any, into <paramref name="newBody"/>.
    /// </summary>
    public void TransferBody(EntityUid oldBody, EntityUid newBody)
    {
        if (TryGetCharacterByBody(oldBody, out var character))
            SetBody(character.Value, newBody);
    }

    /// <summary>
    /// Marks the character as having left the round through the portal.
    /// </summary>
    public void MarkLeftRound(EntityUid mindId, EntityUid body)
    {
        EnsureComp<CERoundLeavedComponent>(mindId);
        RaiseBodyStateChanged(body);
    }

    public bool HasLeftRound(NetUserId player)
    {
        return TryGetCharacter(player, out var character) && _leftQuery.HasComp(character.Value);
    }

    public bool TryGetCharacter(NetUserId player, [NotNullWhen(true)] out Entity<CECharacterComponent, MindComponent>? character)
    {
        // Fast path: the player still controls the character's mind.
        if (_mind.TryGetMind(player, out var current) && _characterQuery.TryComp(current.Value.Owner, out var currentComp))
        {
            character = (current.Value.Owner, currentComp, current.Value.Comp);
            return true;
        }

        // A ghost role gives the player a new mind, so look the character up by its original owner.
        var query = EntityQueryEnumerator<CECharacterComponent, MindComponent>();
        while (query.MoveNext(out var uid, out var comp, out var mind))
        {
            if (mind.OriginalOwnerUserId != player)
                continue;

            character = (uid, comp, mind);
            return true;
        }

        character = null;
        return false;
    }

    public bool TryGetCharacterByBody(EntityUid body, [NotNullWhen(true)] out Entity<CECharacterComponent>? character)
    {
        character = null;
        if (!_bodyQuery.TryComp(body, out var marker) || !_characterQuery.TryComp(marker.Mind, out var comp))
            return false;

        character = (marker.Mind, comp);
        return true;
    }

    /// <summary>
    /// The state of a body on its own, regardless of who controls it. Objectives track bodies, so they use this.
    /// </summary>
    public CEBodyState GetBodyState(EntityUid? body)
    {
        if (body is not { } uid || TerminatingOrDeleted(uid))
            return CEBodyState.Destroyed;

        if (_bodyQuery.TryComp(uid, out var marker) && _leftQuery.HasComp(marker.Mind))
            return CEBodyState.LeftRound;

        // A soul is kept alive only so its AI can walk, there is no way back for the player.
        if (_murkQuery.TryComp(uid, out var dissolving) && dissolving.Converted)
            return CEBodyState.Murked;

        if (!_mobStateQuery.TryComp(uid, out var mobState))
            return CEBodyState.Alive;

        return mobState.CurrentState switch
        {
            MobState.Dead => CEBodyState.Dead,
            MobState.Critical => CEBodyState.Critical,
            _ => CEBodyState.Alive,
        };
    }

    /// <summary>
    /// The state of the body plus where its player is.
    /// </summary>
    public CECharacterStatus GetStatus(Entity<CECharacterComponent, MindComponent> character)
    {
        var body = character.Comp1.Body;
        switch (GetBodyState(body))
        {
            case CEBodyState.Destroyed:
                return CECharacterStatus.Destroyed;
            case CEBodyState.LeftRound:
                return CECharacterStatus.LeftRound;
            case CEBodyState.Murked:
                return CECharacterStatus.Murked;
            case CEBodyState.Dead:
                return CECharacterStatus.Dead;
            case CEBodyState.Critical when character.Comp2.CurrentEntity == body && !IsSSD(body):
                return CECharacterStatus.Critical;
        }

        if (character.Comp2.CurrentEntity != body)
            return CECharacterStatus.LeftBody;

        return IsSSD(body) ? CECharacterStatus.Disconnected : CECharacterStatus.Active;
    }

    /// <summary>
    /// Whether protecting the body counts as done: it lives on, or left the round alive.
    /// </summary>
    public static bool IsSurvived(CEBodyState state)
    {
        return state is CEBodyState.Alive or CEBodyState.Critical or CEBodyState.LeftRound;
    }

    /// <summary>
    /// Whether getting rid of the body counts as done. Leaving the round counts too, so it never blocks a kill objective.
    /// </summary>
    public static bool IsEliminated(CEBodyState state)
    {
        return state is CEBodyState.Dead or CEBodyState.Murked or CEBodyState.Destroyed or CEBodyState.LeftRound;
    }

    /// <summary>
    /// Whether the character is out of play - anything but a player inside a living body.
    /// </summary>
    public static bool IsOut(CECharacterStatus status)
    {
        return status is not (CECharacterStatus.Active or CECharacterStatus.Critical);
    }

    private bool IsSSD(EntityUid? body)
    {
        return body is { } uid && _ssdQuery.TryComp(uid, out var ssd) && ssd.IsSSD;
    }

    private void RaiseBodyStateChanged(EntityUid body)
    {
        var ev = new CEBodyStateChangedEvent(body);
        RaiseLocalEvent(ref ev);
    }
}

/// <summary>
/// State of a body for objectives. See <see cref="CECharacterSystem.GetBodyState"/>.
/// </summary>
public enum CEBodyState : byte
{
    Alive,
    Critical,
    Dead,
    Murked,
    LeftRound,
    Destroyed,
}

/// <summary>
/// State of a character, ordered from in play to completely gone. See <see cref="CECharacterSystem.GetStatus"/>.
/// </summary>
[Serializable, NetSerializable]
public enum CECharacterStatus : byte
{
    Active,
    Critical,
    Disconnected,
    LeftBody,
    Dead,
    Murked,
    LeftRound,
    Destroyed,
}

/// <summary>
/// Broadcast when a body's <see cref="CEBodyState"/> changes in a way a mob state change doesn't cover:
/// dissolving into the murk or leaving the round.
/// </summary>
[ByRefEvent]
public readonly record struct CEBodyStateChangedEvent(EntityUid Body);
