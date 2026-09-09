using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using Content.Shared._CE.GOAP.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Analyzers;

namespace Content.Server._CE.GOAP.Perceptors;

/// <summary>
/// Pain perception. Whenever the NPC takes damage from a known source, that source is added
/// to the knowledge store. Lets mobs react to attackers they didn't visually spot first.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEGOAPPainPerceptorComponent : Component
{
    /// <summary>How long an actual attacker is a threat, even if normally friendly.</summary>
    [DataField]
    public TimeSpan RetaliationDuration;

    [DataField]
    public EntityUid? Attacker;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RetaliationUntil;
}

public sealed partial class CEGOAPPainPerceptorSystem : EntitySystem
{
    [Dependency] private CEGOAPSystem _goap = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnDamaged(Entity<CEGOAPPainPerceptorComponent> ent, ref DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= 0 || args.Origin is not { } source || source == ent.Owner || !Exists(source))
            return;

        ReportThreat(ent, source);
    }

    public void ReportThreat(Entity<CEGOAPPainPerceptorComponent> ent, EntityUid source)
    {
        if (!TryComp<CEGOAPComponent>(ent, out var goap))
            return;

        if (ent.Comp.RetaliationDuration > TimeSpan.Zero)
        {
            ent.Comp.Attacker = source;
            ent.Comp.RetaliationUntil = _timing.CurTime + ent.Comp.RetaliationDuration;
        }

        _goap.Remember((ent.Owner, goap), source, Transform(source).Coordinates, notify: false);
        _goap.RaiseKnowledgeUpdated(ent.Owner);
        goap.NextPlanTime = TimeSpan.Zero;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<CEGOAPPainPerceptorComponent>();
        while (query.MoveNext(out var uid, out var pain))
        {
            if (pain.Attacker is not { } attacker || _timing.CurTime < pain.RetaliationUntil)
                continue;

            pain.Attacker = null;
            _goap.Forget(uid, attacker);
        }
    }
}
