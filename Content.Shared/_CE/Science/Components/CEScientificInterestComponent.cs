using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science.Components;

/// <summary>
/// Marks an entity as something any player can study via an alternative "Study" verb and a
/// do-after to earn research points. Tracks who has already studied it so points are only
/// granted once per player.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEScientificInterestComponent : Component
{
    /// <summary>
    /// Research points granted per essence type upon a successful study. Either set directly in
    /// yaml, or left empty and rolled at spawn by a sibling <see cref="Content.Shared._CE.Science.Components.CEScienceRandomPointsComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> Points = new();

    [DataField, AutoNetworkedField]
    public TimeSpan Time = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> StudiedBy = new();
}
