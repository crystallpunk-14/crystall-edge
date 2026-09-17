using Robust.Shared.GameStates;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Garbles speech in proportion to <see cref="CEMurkDissolvingStatusComponent.Dissolved"/> while
/// present. Removed on conversion into a murked soul (see the murk mob prototype's
/// conversionRemoveComponents), whose speech comes from its own accent instead.
/// Requires a <see cref="CEMurkDissolvingStatusComponent"/> on the same entity to have any effect.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEMurkDissolvingAccentComponent : Component;
