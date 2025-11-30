using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Vampire.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(CESharedVampireSystem))]
public sealed partial class CEVampireClanHeartComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? VampireOwner;

    [DataField, AutoNetworkedField]
    public FixedPoint2 CollectedEssence = 0f;

    [DataField]
    public string LevelPrefix = "orb";

    [DataField]
    public EntProtoId? LevelUpVfx = null; // "CESkyLightningRed"

    [DataField]
    public FixedPoint2 Level2 = 5f;

    [DataField]
    public FixedPoint2 Level3 = 12f;

    [DataField]
    public FixedPoint2 Level4 = 21f;

    [DataField]
    public FixedPoint2 EssenceRegen = 0.2f;

    [DataField]
    public TimeSpan RegenFrequency = TimeSpan.FromMinutes(1);

    [DataField]
    public TimeSpan NextRegenTime = TimeSpan.Zero;

    /// <summary>
    /// For reduce damage announce spamming
    /// </summary>
    [DataField]
    public TimeSpan MaxAnnounceFreq = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// For reduce damage announce spamming
    /// </summary>
    [DataField]
    public TimeSpan NextAnnounceTime = TimeSpan.Zero;

    public int Level
    {
        get
        {
            if (CollectedEssence >= Level4)
                return 4;
            if (CollectedEssence >= Level3)
                return 3;
            if (CollectedEssence >= Level2)
                return 2;
            return 1;
        }
    }

    public FixedPoint2 EssenceFromLevelStart => Level switch
    {
        1 => CollectedEssence,
        2 => CollectedEssence - Level2,
        3 => CollectedEssence - Level3,
        4 => CollectedEssence - Level4,
        _ => FixedPoint2.Zero
    };

    public FixedPoint2? EssenceToNextLevel => Level switch
    {
        1 => Level2,
        2 => Level3 - Level2,
        3 => Level4 - Level3,
        _ => null
    };
}
