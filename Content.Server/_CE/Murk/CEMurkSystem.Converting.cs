using Content.Server.Ghost;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Murk.Systems;
using Content.Shared._CE.Roundflow;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Server._CE.Murk;

public sealed partial class CEMurkSystem
{
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private static readonly SoundSpecifier ConvertSound = new SoundPathSpecifier("/Audio/_CE/Announce/murked.ogg");

    [SubscribeLocalEvent]
    private void OnDissolved(Entity<CEMurkDissolvingComponent> ent, ref CEMurkDissolvedEvent args)
    {
        Convert(ent);
    }

    /// <summary>
    /// Turns a fully dissolved entity into a murked soul: the player is cast out of the body and
    /// what's left keeps wandering the murk on its own. There is no way back from this.
    /// </summary>
    public void Convert(Entity<CEMurkDissolvingComponent> ent)
    {
        if (ent.Comp.Converted)
            return;

        ent.Comp.Converted = true;
        DirtyField(ent.Owner, ent.Comp, nameof(CEMurkDissolvingComponent.Converted));

        _metaData.SetEntityName(ent, Loc.GetString("ce-murk-soul-name", ("name", Name(ent))));

        Evict(ent);

        EntityManager.RemoveComponents(ent, ent.Comp.ConversionRemoveComponents);
        EntityManager.AddComponents(ent, ent.Comp.ConversionComponents);

        // A body that dissolved while down would stay down, and the AI refuses to wake up in an
        // incapacitated mob. With damage gone there is nothing left to put it back in crit.
        if (HasComp<MobStateComponent>(ent))
            _mobState.ChangeMobState(ent, MobState.Alive);

        Phase(ent.Owner);
    }

    /// <summary>
    /// Casts the player out of the husk the same way the ghost command would, except nothing gets
    /// to cancel it and there is nothing left to return to.
    /// </summary>
    private void Evict(EntityUid uid)
    {
        if (TryComp<ActorComponent>(uid, out var actor))
        {
            RaiseNetworkEvent(
                new CEScreenPopupShowEvent(Loc.GetString("ce-murk-soul-lost-title"), audioPath: ConvertSound),
                actor.PlayerSession);
        }

        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _ghost.OnGhostAttempt(mindId, false, viaCommand: true, forced: true, mind: mind);
    }

    /// <summary>
    /// Makes the soul non-solid, so the living walk straight through it.
    /// </summary>
    private void Phase(Entity<FixturesComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var (_, fixture) in ent.Comp.Fixtures)
        {
            _physics.SetHard(ent, fixture, false, ent.Comp);
        }
    }
}
