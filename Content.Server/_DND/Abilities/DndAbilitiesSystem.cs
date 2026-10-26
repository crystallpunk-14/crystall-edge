using Content.Shared._DND.Abilities;
using Content.Shared._DND.Backgrounds;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server._DND.Abilities;

/// <summary>
/// Copies character-creation ability scores onto the body. NPC never reaches this event.
/// </summary>
public sealed partial class DndAbilitiesSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    [SubscribeLocalEvent]
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var comp = EnsureComp<DndAbilitiesComponent>(ev.Mob);
        comp.Level = DndAbilityScores.StartingLevel;
        comp.Strength = FinalScore(ev.Profile, DndAbilityPrototype.StrengthId);
        comp.Dexterity = FinalScore(ev.Profile, DndAbilityPrototype.DexterityId);
        comp.Constitution = FinalScore(ev.Profile, DndAbilityPrototype.ConstitutionId);
        comp.Intelligence = FinalScore(ev.Profile, DndAbilityPrototype.IntelligenceId);
        comp.Wisdom = FinalScore(ev.Profile, DndAbilityPrototype.WisdomId);
        comp.Charisma = FinalScore(ev.Profile, DndAbilityPrototype.CharismaId);
        Dirty(ev.Mob, comp);

        var background = EnsureComp<DndBackgroundComponent>(ev.Mob);
        background.Background = ev.Profile.DndBackground;
        Dirty(ev.Mob, background);
    }

    private int FinalScore(HumanoidCharacterProfile profile, string abilityId)
    {
        var baseScore = profile.GetAbilityScore(abilityId, 10);
        if (!_prototypes.TryIndex(profile.DndBackground, out DndBackgroundPrototype? background))
            return baseScore;

        var bonus = DndBackgroundScores.Bonus(
            abilityId,
            background.Abilities,
            profile.BackgroundIncrease,
            profile.BackgroundPlusTwo.Id,
            profile.BackgroundPlusOne.Id);
        return DndBackgroundScores.FinalScore(baseScore, bonus);
    }
}
