using Robust.Shared.Player;

namespace Content.Server._CE.ZLevels.Components;

/// <summary>
/// Renders entities in PVS around itself for the specified session. Used to see “down” through Z-levels.
/// </summary>
[RegisterComponent]
public sealed partial class CEZLevelEyeComponent : Component
{
    public ICommonSession? Target = default!;
}
