using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.Objectives.Components;
using Content.Shared.Mind;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Objectives;

public abstract partial class CESharedObjectiveSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedPvsOverrideSystem _pvsOverride = default!;

    /// <summary>
    /// Returns every objective on a holder.
    /// </summary>
    public List<Entity<CEObjectiveComponent>> GetObjectives(Entity<CEObjectiveHolderComponent?> holder)
    {
        if (!Resolve(holder, ref holder.Comp, false))
            return [];

        var objectives = new List<Entity<CEObjectiveComponent>>();
        foreach (var objective in holder.Comp.Objectives)
        {
            if (TryComp<CEObjectiveComponent>(objective, out var comp))
                objectives.Add((objective, comp));
        }

        return objectives;
    }

    /// <summary>
    /// Spawns an objective from a prototype and adds it to a holder, PVS-overriding it to the
    /// holder's owning client (if any) so it shows up in their character menu.
    /// </summary>
    public bool TryCreateObjective(
        EntityUid holderUid,
        EntProtoId protoId,
        [NotNullWhen(true)] out Entity<CEObjectiveComponent>? objective)
    {
        objective = null;

        var holderComp = EnsureComp<CEObjectiveHolderComponent>(holderUid);
        var uid = Spawn(protoId, MapCoordinates.Nullspace);
        if (!TryComp<CEObjectiveComponent>(uid, out var comp))
        {
            Del(uid);
            Log.Error($"Invalid CE objective prototype {protoId}, missing CEObjectiveComponent");
            return false;
        }

        objective = (uid, comp);

        var ev = new CEInitializeObjectiveEvent((holderUid, holderComp));
        RaiseLocalEvent(uid, ref ev);

        AddObjective(holderUid, holderComp, uid);
        RefreshObjectiveProgress(objective.Value.AsNullable());

        return true;
    }

    /// <summary>
    /// Adds an already-created objective to a holder without spawning a new one - used to hand a
    /// shared (department) objective to another member who wasn't there when it was first drawn.
    /// </summary>
    public void AddObjective(EntityUid holderUid, CEObjectiveHolderComponent holderComp, EntityUid objective)
    {
        if (holderComp.Objectives.Contains(objective))
            return;

        holderComp.Objectives.Add(objective);
        Dirty(holderUid, holderComp);

        if (TryGetHolderSession(holderUid, out var session))
            _pvsOverride.AddSessionOverride(objective, session);

        var ev = new CEObjectivesChangedEvent(holderUid);
        RaiseLocalEvent(holderUid, ref ev);
    }

    public bool TryRemoveObjective(EntityUid holderUid, EntityUid objective)
    {
        if (!TryComp<CEObjectiveHolderComponent>(holderUid, out var holderComp) ||
            !holderComp.Objectives.Remove(objective))
            return false;

        Dirty(holderUid, holderComp);

        if (TryGetHolderSession(holderUid, out var session))
            _pvsOverride.RemoveSessionOverride(objective, session);

        Del(objective);

        var ev = new CEObjectivesChangedEvent(holderUid);
        RaiseLocalEvent(holderUid, ref ev);
        return true;
    }

    /// <summary>
    /// Looks up the session currently attached to a holder (assumed to be a mind entity), if any -
    /// used to keep PVS overrides in sync as objectives are added/removed.
    /// </summary>
    private bool TryGetHolderSession(EntityUid holderUid, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;
        return TryComp<MindComponent>(holderUid, out var mind) &&
               mind.UserId is { } userId &&
               _player.TryGetSessionById(userId, out session);
    }

    /// <summary>
    /// Re-applies PVS overrides for every objective a holder has to a (re)connected session -
    /// needed for a player who reconnects to a mind that already had objectives.
    /// </summary>
    protected void RefreshHolderOverrides(EntityUid holderUid, ICommonSession session, bool add)
    {
        if (!TryComp<CEObjectiveHolderComponent>(holderUid, out var holderComp))
            return;

        foreach (var objective in holderComp.Objectives)
        {
            if (add)
                _pvsOverride.AddSessionOverride(objective, session);
            else
                _pvsOverride.RemoveSessionOverride(objective, session);
        }
    }

    /// <summary>
    /// Queries a CE objective to determine its current progress.
    /// </summary>
    public void RefreshObjectiveProgress(Entity<CEObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var ev = new CEGetObjectiveProgressEvent();
        RaiseLocalEvent(ent, ref ev);

        var oldProgress = ent.Comp.Progress;
        var newProgress = Math.Clamp(ev.Progress, 0f, 1f);

        if (MathHelper.CloseTo(oldProgress, newProgress))
            return;

        ent.Comp.Progress = newProgress;
        Dirty(ent);

        var changedEv = new CEObjectiveProgressChangedEvent((ent, ent.Comp), oldProgress, newProgress);
        RaiseLocalEvent(ent, ref changedEv, true);
    }

    public float GetProgress(Entity<CEObjectiveComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp) ? ent.Comp.Progress : 0f;
    }

    public bool IsCompleted(Entity<CEObjectiveComponent?> ent)
    {
        return GetProgress(ent) >= 0.999f;
    }

    /// <summary>
    /// Sets the badge shown on an objective's card in the character menu.
    /// </summary>
    public void SetDescriptor(EntityUid uid, LocId name, Color color, LocId? tooltip = null)
    {
        var comp = EnsureComp<CEObjectiveDescriptorComponent>(uid);
        comp.Name = name;
        comp.Color = color;
        comp.Tooltip = tooltip;
        Dirty(uid, comp);
    }
}
