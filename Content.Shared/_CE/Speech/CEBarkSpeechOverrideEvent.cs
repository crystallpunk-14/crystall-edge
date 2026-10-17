using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Speech;

/// <summary>
/// Raised on a speaking entity (and relayed to its worn equipment) before a bark is played,
/// allowing handlers to replace the bark voice profile and pitch.
/// </summary>
[ByRefEvent]
public record struct CEBarkSpeechOverrideEvent(ProtoId<CEBarkSpeechPrototype> BarkSpeech, float BasePitch) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
