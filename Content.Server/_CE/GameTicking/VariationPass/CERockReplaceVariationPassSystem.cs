using Content.Server._CE.GameTicking.VariationPass.Components;
using Content.Server._CE.GameTicking.VariationPass.Components.ReplacementMarkers;
using Content.Server.GameTicking.Rules.VariationPass;

namespace Content.Server._CE.GameTicking.VariationPass;

/// <summary>
/// This handles the ability to replace entities marked with <see cref="CERockReplacementMarkerComponent"/> in a variation pass
/// </summary>
public sealed class CERockReplaceVariationPassSystem : BaseEntityReplaceVariationPassSystem<CERockReplacementMarkerComponent, CERockReplaceVariationPassComponent>
{
}
