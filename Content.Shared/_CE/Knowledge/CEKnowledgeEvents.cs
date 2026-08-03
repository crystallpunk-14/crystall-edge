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
