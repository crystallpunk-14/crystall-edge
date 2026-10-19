using Content.Shared.Chat;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Makes the entity say a phrase made up by whatever phrase generator it has, addressed to the effect's target.
/// </summary>
public sealed partial class SayGeneratedPhrase : CEEntityEffectBase<SayGeneratedPhrase>
{
    public SayGeneratedPhrase()
    {
        EffectTarget = CEEffectTarget.User;
    }

    [DataField]
    public InGameICChatType ChatType = InGameICChatType.Speak;

    [DataField]
    public ChatTransmitRange Range = ChatTransmitRange.Normal;
}
