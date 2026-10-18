using Robust.Shared.GameStates;

namespace Content.Shared._CE.Roles.SecretRoleSeer;

/// <summary>
/// Lives on a mind and remembers every mind it has gazed into. Their bodies' secret roles stay
/// recognizable to this mind for the rest of the round.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CESoulGazeMemoryComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Minds = new();
}
