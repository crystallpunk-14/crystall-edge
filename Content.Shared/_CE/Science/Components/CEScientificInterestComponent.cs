using Robust.Shared.GameStates;

namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Marks an entity as something any player can study via an alternative "Study" verb and a
/// do-after to earn research points. Tracks who has already studied it so points are only
/// granted once per player.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEScientificInterestComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public int Points;

    [DataField, AutoNetworkedField]
    public TimeSpan Time = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> StudiedBy = new();
}
