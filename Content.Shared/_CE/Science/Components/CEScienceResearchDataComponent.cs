using Content.Shared._CE.Science.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Tracks which map coordinates this entity has researched, independently for each science area,
/// how many research points it currently has to spend on research actions, and which achievements
/// it has actually discovered (as opposed to merely revealed on the map). All three fields are
/// networked so the owning client always has this locally (no extra BUI round-trip needed to read
/// it), and so <see cref="Content.Shared._CE.Workbench.Requirements.ResearchPointResource"/> can
/// be checked client-side (e.g. for live workbench recipe filtering).
/// </summary>
/// <remarks>
/// Whoever mutates <see cref="Researched"/>, <see cref="Points"/>, or
/// <see cref="DiscoveredAchievements"/> must call <c>Dirty(uid, component)</c> afterwards -
/// AutoNetworkedField only controls how the auto-generated state is (de)serialized, it does not
/// detect field writes on its own.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEScienceResearchDataComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEScienceAreaPrototype>, HashSet<Vector2i>> Researched = new();

    [DataField, AutoNetworkedField]
    public int Points = 10;

    /// <summary>
    /// Achievements this entity has completed the "discover achievement" research action for.
    /// A revealed (researched) achievement cell only shows its icon in full colour once its
    /// achievement is in this set - otherwise it's drawn tinted black.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CEScienceAchievementPrototype>> DiscoveredAchievements = new();
}
