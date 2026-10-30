using Content.Shared._CE.NewPlayer;

namespace Content.Client._CE.NewPlayer;

/// <summary>
/// The component only reaches clients allowed to see it, so the shared rules are enough here.
/// </summary>
public sealed partial class CENewPlayerSystem : CESharedNewPlayerSystem;
