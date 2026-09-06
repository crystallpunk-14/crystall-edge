using Content.Server._CE.GOAP.Navigation;
using Content.Shared._CE.GOAP.Selectors;

namespace Content.Server._CE.GOAP.Selectors;

/// <summary>
/// Named, prototype-authored target policies shared by sensors and actions on
/// the same agent. Profiles are restricted to backoff-capable entity selectors
/// so wrapping them cannot change failure semantics.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPSelectorProfilesComponent : Component
{
    [DataField, AlwaysPushInheritance]
    public Dictionary<string, CEGOAPTargetSelector> Profiles = new();
}

/// <summary>
/// Resolves one concrete selector from <see cref="CEGOAPSelectorProfilesComponent"/>.
/// </summary>
[DataDefinition]
public sealed partial class CEGOAPSelectorProfile
    : CEGOAPTargetSelectorBase<CEGOAPSelectorProfile>, ICEGOAPTargetBackoffSelector
{
    [DataField(required: true)]
    public string Profile = string.Empty;
}

public sealed partial class CEGOAPSelectorProfileSystem
    : CEGOAPTargetSelectorSystem<CEGOAPSelectorProfile>
{
    protected override void Resolve(ref CEGOAPSelectorResolveEvent<CEGOAPSelectorProfile> ev)
    {
        if (!TryResolveSelector(ev.Agent, ev.Selector, out var selector))
            return;

        var result = selector.Resolve(ev.Agent, EntityManager);
        ev.Entity = result.Entity;
        ev.Position = result.Position;
    }

    public bool TryResolveSelector(
        EntityUid agent,
        CEGOAPTargetSelector? selector,
        out CEGOAPTargetSelector resolved)
    {
        resolved = null!;
        if (selector is not CEGOAPSelectorProfile profile)
        {
            if (selector == null)
                return false;

            resolved = selector;
            return true;
        }

        if (string.IsNullOrWhiteSpace(profile.Profile) ||
            !TryComp<CEGOAPSelectorProfilesComponent>(agent, out var profiles) ||
            !profiles.Profiles.TryGetValue(profile.Profile, out var concrete) ||
            concrete is CEGOAPSelectorProfile or not ICEGOAPTargetBackoffSelector)
            return false;

        resolved = concrete;
        return true;
    }
}
