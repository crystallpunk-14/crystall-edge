using Robust.Shared.GameStates;

namespace Content.Shared._DND.Features;

/// <summary>
/// Uses left, and whether a rage is currently up. While it is up, weapon blows are resisted and Strength attacks hit harder.
/// Spells and concentration are not available for the duration.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DndRageComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public int Uses;

    [DataField, AutoNetworkedField]
    public int MaxUses;

    [DataField, AutoNetworkedField]
    public int DamageBonus;

    [DataField, AutoNetworkedField]
    public TimeSpan StartedAt;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;

    [DataField, AutoNetworkedField]
    public TimeSpan MaxEnd;

    /// <summary>
    /// The rage action entity, so a rest can update the use count shown on it.
    /// </summary>
    [DataField]
    public EntityUid? Action;
}
