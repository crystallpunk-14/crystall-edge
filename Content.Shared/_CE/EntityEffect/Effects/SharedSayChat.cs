using Content.Shared.Chat;
using Robust.Shared.Prototypes;

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

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-say-chat", ("sentence", Loc.GetString(Sentence)));
}
