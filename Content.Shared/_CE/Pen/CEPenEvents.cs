using Content.Shared._CE.Knowledge.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Pen;

/// <summary>
/// Kind of action a pen can offer, picked either automatically (when only one is available)
/// or by the player from a radial menu.
/// </summary>
public enum CEPenActionKind : byte
{
    Write,
    RecordKnowledge,
}

/// <summary>
/// A single pen action offered to the player, as collected by <see cref="CEGetPenActionsEvent"/>.
/// </summary>
public readonly record struct CEPenAction(CEPenActionKind Kind, LocId Name, SpriteSpecifier? Icon = null);

/// <summary>
/// Raised on the user, the interaction target, and the pen itself (in that order) when a
/// <see cref="CEPenComponent"/> is used on another entity, to collect the list of pen actions
/// any of those three entities want to offer. Any number of components can subscribe and add
/// their own option to <see cref="Actions"/> - mirrors the pattern used by GetVerbsEvent.
/// </summary>
public sealed class CEGetPenActionsEvent(EntityUid user, EntityUid pen, EntityUid target) : EntityEventArgs
{
    public readonly EntityUid User = user;
    public readonly EntityUid Pen = pen;
    public readonly EntityUid Target = target;

    public readonly List<CEPenAction> Actions = new();
}

[Serializable, NetSerializable]
public enum CEPenActionsUiKey : byte
{
    Key
}

/// <summary>
/// Sent by the client when the player picks a pen action from the radial menu (or its nested
/// knowledge submenu, for <see cref="CEPenActionKind.RecordKnowledge"/>).
/// </summary>
[Serializable, NetSerializable]
public sealed class CEPenActionsMessage(
    CEPenActionKind kind,
    ProtoId<CEKnowledgePrototype>? knowledge = null)
    : BoundUserInterfaceMessage
{
    public readonly CEPenActionKind Kind = kind;
    public readonly ProtoId<CEKnowledgePrototype>? Knowledge = knowledge;
}

/// <summary>
/// Raised on the user (where <c>CEKnowledgeComponent</c> lives) to request starting the "record
/// knowledge" do-after, once the player has picked which knowledge to write down.
/// </summary>
public sealed class CEPenRecordKnowledgeRequestEvent(
    EntityUid pen,
    EntityUid target,
    ProtoId<CEKnowledgePrototype> knowledge)
    : EntityEventArgs
{
    public readonly EntityUid Pen = pen;
    public readonly EntityUid Target = target;
    public readonly ProtoId<CEKnowledgePrototype> Knowledge = knowledge;
}

/// <summary>
/// DoAfter fired when recording a piece of knowledge into a book with a pen finishes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CEPenRecordKnowledgeDoAfterEvent : SimpleDoAfterEvent
{
    public readonly ProtoId<CEKnowledgePrototype> Knowledge;

    public CEPenRecordKnowledgeDoAfterEvent(ProtoId<CEKnowledgePrototype> knowledge)
    {
        Knowledge = knowledge;
    }
}
