using Robust.Shared.GameStates;

namespace Content.Shared._CE.Skill.Components;

/// <summary>
/// Marks a status effect (see <c>CEStatusEffectKnowledgeCopying</c>) as granting its owner the
/// ability to copy known skills into a book with a pen. Checked via
/// <c>StatusEffectsSystem.HasEffectComp</c> instead of a hardcoded skill id, so any status effect
/// - not just the <c>KnowledgeCopyingTechniques</c> skill - can grant the ability.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEKnowledgeCopyingAbilityComponent : Component;
