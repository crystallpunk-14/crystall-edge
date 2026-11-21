using Content.Shared._CE.Medicine.Symptom;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._CE.Medicine.Symptom;

/// <summary>
/// Assembles symptom status effects from every applied <see cref="CESymptomSourceComponent"/> on the bearer:
/// grants missing symptoms, removes assembled symptoms no longer needed, leaves shared ones untouched.
/// </summary>
public sealed partial class CESymptomSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    [Dependency] private EntityQuery<CESymptomSourceComponent> _sourceQuery = default!;
    [Dependency] private EntityQuery<CEAutoSymptomComponent> _assembledQuery = default!;

    private static readonly TimeSpan UpdateFrequency = TimeSpan.FromSeconds(2);

    private TimeSpan _nextUpdate;

    private readonly List<EntityUid> _bearers = new();
    private readonly HashSet<EntProtoId> _needed = new();
    private readonly List<EntityUid> _refreshed = new();

    [SubscribeLocalEvent]
    private void OnSourceApplied(Entity<CESymptomSourceComponent> ent, ref StatusEffectAppliedEvent args)
    {
        DirtySymptom(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnSourceRemoved(Entity<CESymptomSourceComponent> ent, ref StatusEffectRemovedEvent args)
    {
        DirtySymptom(args.Target);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateFrequency;

        _bearers.Clear();
        var query = EntityQueryEnumerator<CESymptomDirtyComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _bearers.Add(uid);
        }

        foreach (var bearer in _bearers)
        {
            RemComp<CESymptomDirtyComponent>(bearer);
            RefreshSymptoms(bearer);
        }
    }

    private void DirtySymptom(EntityUid bearer)
    {
        if (TerminatingOrDeleted(bearer))
            return;

        EnsureComp<CESymptomDirtyComponent>(bearer);
    }

    private void RefreshSymptoms(EntityUid bearer)
    {
        _needed.Clear();
        _refreshed.Clear();

        foreach (var effect in _statusEffects.EnumerateStatusEffects(bearer))
        {
            if (_assembledQuery.HasComp(effect))
            {
                _refreshed.Add(effect);
                continue;
            }

            if (!effect.Comp.Applied || !_sourceQuery.TryComp(effect, out var source))
                continue;

            foreach (var symptom in source.Symptoms)
            {
                _needed.Add(symptom);
            }
        }

        // Assembled symptoms still needed stay as they are, the rest are removed
        foreach (var symptom in _refreshed)
        {
            if (Prototype(symptom) is { } proto && _needed.Remove(proto.ID))
                continue;

            QueueDel(symptom);
        }

        foreach (var symptom in _needed)
        {
            // Granted by someone else, not ours to manage
            if (_statusEffects.HasStatusEffect(bearer, symptom))
                continue;

            if (_statusEffects.TrySetStatusEffectDuration(bearer, symptom, out var effect))
                EnsureComp<CEAutoSymptomComponent>(effect.Value);
        }
    }
}
