using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    /// Spawns an objective from a prototype and adds it to a holder's
    /// <see cref="CEObjectiveHolderComponent.OwnedObjectives"/>.
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

        holderComp.OwnedObjectives.Add(uid);
        RegenerateObjectiveList((holderUid, holderComp));
        RefreshObjectiveProgress(objective.Value.AsNullable());

        return true;
    }

    /// <summary>
    /// Removes an objective from a holder and deletes the entity. Only works for an objective the
    /// holder actually owns (see <see cref="CEObjectiveHolderComponent.OwnedObjectives"/>) - a
    /// shared objective owned by something else (e.g. a secret department) can't be deleted this
    /// way, since other holders may still reference it. It drops out of a holder's own list on its
    /// own the next time <see cref="RegenerateObjectiveList"/> runs and stops picking it up.
    /// </summary>
    public bool TryRemoveObjective(EntityUid holderUid, EntityUid objective)
    {
        if (!TryComp<CEObjectiveHolderComponent>(holderUid, out var holderComp) ||
            !holderComp.OwnedObjectives.Remove(objective))
            return false;

        RegenerateObjectiveList((holderUid, holderComp));
        Del(objective);
        return true;
    }

    /// <summary>
    /// Recomputes a holder's full <see cref="CEObjectiveHolderComponent.Objectives"/> list -
    /// <see cref="CEObjectiveHolderComponent.OwnedObjectives"/> plus whatever
    /// <see cref="CEGetAdditionalObjectivesEvent"/> contributes - and syncs PVS overrides for
    /// whatever was added or removed. Call this whenever something that could change the result of
    /// that event happens (e.g. a secret role/department membership change).
    /// </summary>
    public void RegenerateObjectiveList(Entity<CEObjectiveHolderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var oldObjectives = new List<EntityUid>(ent.Comp.Objectives);
        var newObjectives = new List<EntityUid>();

        var ev = new CEGetAdditionalObjectivesEvent((ent, ent.Comp), []);
        RaiseLocalEvent(ent, ref ev);

        newObjectives.AddRange(ev.Objectives.Select(e => e.Owner));
        newObjectives.AddRange(ent.Comp.OwnedObjectives);

        var added = newObjectives.Except(oldObjectives).ToList();
        var removed = oldObjectives.Except(newObjectives).ToList();

        if (added.Count == 0 && removed.Count == 0)
            return;

        ent.Comp.Objectives = newObjectives;
        Dirty(ent);

        if (TryGetHolderSession(ent.Owner, out var session))
        {
            foreach (var obj in added)
                _pvsOverride.AddSessionOverride(obj, session);

            foreach (var obj in removed)
                _pvsOverride.RemoveSessionOverride(obj, session);
        }

        var changedEv = new CEObjectivesChangedEvent(ent.Owner);
        RaiseLocalEvent(ent.Owner, ref changedEv);
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
    /// Formats an objective's name, completion state, and progress into a single localized line
    /// for round-end summaries.
    /// </summary>
    public string GetObjectiveString(Entity<CEObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return string.Empty;

        return Loc.GetString("ce-objective-summary-fmt",
            ("name", Name(ent)),
            ("success", IsCompleted(ent)),
            ("percent", (int) (GetProgress(ent) * 100)));
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
