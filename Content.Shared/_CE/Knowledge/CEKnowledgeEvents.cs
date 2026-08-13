using Content.Shared._CE.Knowledge.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Knowledge;

/// <summary>
/// Raised whenever an entity learns a new piece of knowledge (through any means - research,
/// reading a book, etc.), so domain systems can react (e.g. science revealing the achievement's
/// map cell) without <see cref="CESharedKnowledgeSystem"/> needing to know about them.
/// </summary>
[ByRefEvent]
public readonly record struct CEKnowledgeLearnedEvent(EntityUid Entity, ProtoId<CEKnowledgePrototype> Knowledge);

/// <summary>
/// DoAfter fired when reading a <see cref="Components.CEKnowledgeHolderComponent"/> item finishes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CEKnowledgeReadDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Broadcast query raised by <see cref="CEKnowledgeSystem.GetKnowledgeEffectDescription"/> to collect
/// what a piece of knowledge unlocks. Every system that gates something behind knowledge (recipes,
/// achievements, etc.) should subscribe and append its own description to <see cref="Effects"/> when
/// it recognizes <see cref="Knowledge"/>.
/// </summary>
[ByRefEvent]
public readonly record struct CEGetKnowledgeEffectEvent(ProtoId<CEKnowledgePrototype> Knowledge, List<string> Effects);
