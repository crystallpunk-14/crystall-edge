using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CE.Achievements.Prototypes;

[Prototype("achievement")]
public sealed partial class CEAchievementPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Display name of the achievement.
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// Description of the achievement.
    /// </summary>
    [DataField(required: true)]
    public LocId Desc = string.Empty;

    [DataField]
    public SpriteSpecifier LockedIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Achievements/_default.rsi"), "tester_0");

    [DataField]
    public SpriteSpecifier UnlockedIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Achievements/_default.rsi"), "tester_1");

    [DataField]
    public SpriteSpecifier SecretIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_CE/Achievements/_default.rsi"), "secret");

    [DataField]
    public bool Secret = false;
}
