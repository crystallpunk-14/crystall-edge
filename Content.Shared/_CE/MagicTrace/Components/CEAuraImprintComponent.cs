using Robust.Shared.GameStates;

namespace Content.Shared._CE.MagicTrace.Components;

/// <summary>
/// A unique magical "fingerprint" of this creature. It is visible to anyone with magic vision when
/// examining the creature, and is left behind on every <see cref="CEMagicTraceComponent"/> it causes,
/// which lets investigators match a trace to its author.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEAuraImprintComponent : Component
{
    /// <summary>
    /// The generated imprint. Rolled on map init by the server.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Imprint = string.Empty;

    [DataField]
    public int ImprintLength = 8;

    [DataField, AutoNetworkedField]
    public Color ImprintColor = Color.White;
}
