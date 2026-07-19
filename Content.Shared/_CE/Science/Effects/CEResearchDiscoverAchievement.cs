namespace Content.Shared._CE.Science.Effects;

/// <summary>
/// Marks the achievement at the action's coordinate as discovered for the acting player, and runs
/// that achievement's own effects (e.g. <c>LearnRecipe</c>) - the same effects a physical
/// <see cref="Content.Shared._CE.Science.Components.CEScienceAchievementHolderComponent"/> item
/// applies when read.
/// </summary>
public sealed partial class CEResearchDiscoverAchievement : CEResearchActionEffectBase<CEResearchDiscoverAchievement>
{
}
