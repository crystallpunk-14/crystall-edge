using Robust.Shared.GameStates;

namespace Content.Shared._White.AuraImrint;

/// <summary>
/// A component that stores a “blueprint” of the aura, unique to each mind.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(WhiteSharedAuraImprintSystem))]
public sealed partial class WhiteAuraImprintComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Imprint = string.Empty;

    [DataField]
    public int ImprintLength = 8;

    [DataField]
    public Color ImprintColor = Color.White;
}
