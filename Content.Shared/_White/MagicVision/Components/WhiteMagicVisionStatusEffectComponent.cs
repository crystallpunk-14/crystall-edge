using Content.Shared.Eye;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._White.MagicVision.Components;

/// <summary>
/// Allows to see magic vision trace entities
/// Use only in conjunction with <see cref="StatusEffectComponent"/>, on the status effect entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WhiteMagicVisionStatusEffectComponent : Component
{
    /// <summary>
    /// VisionMask to see Magic Vision layer
    /// </summary>
    public const VisibilityFlags VisibilityMask = VisibilityFlags.WhiteMagicVision;
}
