using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Roof;

/// <summary>
/// Marks an entity as a roof, allowing you to hide all roofs or show them back depending on different situations
/// </summary>
[RegisterComponent]
public sealed partial class CERoofComponent : Component
{
}

/// <summary>
/// allows you to switch the visibility of roofs using a special action
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CERoofTogglerComponent : Component
{
    [DataField]
    public EntProtoId ActionProto = "CEActionToggleRoofs";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}

public sealed partial class CEToggleRoofVisibilityAction : InstantActionEvent
{
}
