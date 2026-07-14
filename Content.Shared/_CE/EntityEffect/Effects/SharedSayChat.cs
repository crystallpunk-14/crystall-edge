using Content.Shared.Chat;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class SayChat : CEEntityEffectBase<SayChat>
{
    public SayChat()
    {
        EffectTarget = CEEffectTarget.User;
    }

    /// <summary>
    /// A message spoken by a character. Will automatically attempt to use it as LocId, but you can also insert regular text.
    /// </summary>
    [DataField(required: true)]
    public string Sentence = default!;

    [DataField]
    public InGameICChatType ChatType = InGameICChatType.Speak;
}
