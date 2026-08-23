using Content.Shared._CE.GOAP.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server._CE.GOAP.Perceptors;

/// <summary>
/// Pain perception. Whenever the NPC takes damage from a known source, that source is added
/// to the knowledge store. Lets mobs react to attackers they didn't visually spot first.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPPainPerceptorComponent : Component
{
}

public sealed partial class CEGOAPPainPerceptorSystem : EntitySystem
{
    [Dependency] private CEGOAPSystem _goap = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEGOAPPainPerceptorComponent, DamageDealtEvent>(OnDamaged);
    }

    private void OnDamaged(Entity<CEGOAPPainPerceptorComponent> ent, ref DamageDealtEvent args)
    {
        if (args.Origin is not { } source || source == ent.Owner)
            return;

        if (!TryComp<CEGOAPComponent>(ent, out var goap))
            return;

        _goap.Remember((ent.Owner, goap), source, Transform(source).Coordinates);
    }
}
