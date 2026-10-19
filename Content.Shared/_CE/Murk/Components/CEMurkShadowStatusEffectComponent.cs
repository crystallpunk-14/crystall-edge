namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Status effect that renders the affected entity as a fully dissolved murk shadow while it lasts.
/// Purely visual - independent from the actual murk dissolution of <see cref="CEMurkDissolvingStatusComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CEMurkShadowStatusEffectComponent : Component;
