using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared._DND.Abilities;
using Content.Shared._DND.Classes;
using Content.Shared._DND.Combat;
using Content.Shared._DND.Features;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Lobby.UI;

public sealed partial class CEHumanoidProfileEditor
{
    private readonly ButtonGroup _abilityMethodGroup = new(isNoneSetAllowed: false);
    private bool _dndAbilityMethodsReady;
    private bool _updatingAbilityMethod;

    /// <summary>
    /// Hides the role, trait, and flavor-text menus. The controls stay in the tree.
    /// </summary>
    private void ApplyDndCharacterCreationMenus()
    {
        TabContainer.SetTabVisible(RolesTab, false);
        TabContainer.SetTabVisible(TraitsTab, false);
        TabContainer.SetTabTitle(DndClassTab, Loc.GetString("dnd-character-creation-class-tab"));
        TabContainer.SetTabTitle(DndAbilitiesTab, Loc.GetString("dnd-character-creation-tab"));
    }

    private void RefreshDndAbilities()
    {
        SetupDndAbilityMethods();

        var method = Profile?.AbilityGeneration ?? DndAbilityGeneration.PointBuy;
        _updatingAbilityMethod = true;
        DndPointBuyButton.Pressed = method == DndAbilityGeneration.PointBuy;
        DndStandardArrayButton.Pressed = method == DndAbilityGeneration.StandardArray;
        _updatingAbilityMethod = false;

        DndAbilityList.RemoveAllChildren();

        var abilities = _prototypeManager.EnumeratePrototypes<DndAbilityPrototype>()
            .OrderBy(ability => ability.Order)
            .ThenBy(ability => ability.ID);

        foreach (var ability in abilities)
            DndAbilityList.AddChild(BuildAbilityRow(ability, method));

        AddDndBackgroundSection();
        UpdateAbilityMethodSummary(method);
        UpdateDerivedStats();
    }

    private void SetupDndAbilityMethods()
    {
        if (_dndAbilityMethodsReady)
            return;

        _dndAbilityMethodsReady = true;
        DndPointBuyButton.Group = _abilityMethodGroup;
        DndStandardArrayButton.Group = _abilityMethodGroup;
        DndPointBuyButton.OnPressed += _ => SetAbilityGeneration(DndAbilityGeneration.PointBuy);
        DndStandardArrayButton.OnPressed += _ => SetAbilityGeneration(DndAbilityGeneration.StandardArray);
    }

    private void SetAbilityGeneration(DndAbilityGeneration method)
    {
        if (_updatingAbilityMethod || Profile is null || Profile.AbilityGeneration == method)
            return;

        var abilityIds = AbilityIds();
        var stored = ReadStoredScores(abilityIds);
        var normalized = method == DndAbilityGeneration.StandardArray
            ? DndAbilityScores.NormalizeStandardArray(abilityIds, stored)
            : DndAbilityScores.NormalizePointBuy(abilityIds, stored);

        Profile = Profile.WithAbilityGeneration(method).WithAbilityScores(ToScoreMap(normalized));
        SetDirty();
        RefreshDndAbilities();
    }

    private BoxContainer BuildAbilityRow(DndAbilityPrototype ability, DndAbilityGeneration method)
    {
        var score = DisplayScore(ability.ID, method);
        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(0, 8, 0, 4),
        };

        root.AddChild(new Label
        {
            Text = Loc.GetString(ability.Name),
            StyleClasses = { StyleClass.LabelHeading },
        });

        root.AddChild(new Label
        {
            Text = Loc.GetString(ability.Description),
            FontColorOverride = Color.Gray,
        });

        var summary = new Label
        {
            Text = FormatAbilitySummary(ability.ID, score),
            Margin = new Thickness(0, 2, 0, 2),
        };
        root.AddChild(summary);

        if (method == DndAbilityGeneration.StandardArray)
            root.AddChild(BuildStandardArrayPicker(ability.ID, score));
        else
            root.AddChild(BuildPointBuyPicker(ability.ID, score, summary));

        return root;
    }

    private BoxContainer BuildPointBuyPicker(string abilityId, int score, Label summary)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 12,
        };

        var cost = new Label
        {
            Text = PointBuyCostText(score),
            FontColorOverride = Color.Gray,
            VerticalAlignment = VAlignment.Center,
            MinWidth = 90,
        };

        var spin = new SpinBox
        {
            HorizontalAlignment = HAlignment.Left,
            MinSize = new Vector2(140, 0),
        };
        spin.IsValid = value => CanAffordPointBuy(abilityId, value);
        spin.InitDefaultButtons();
        spin.OverrideValue(score);
        spin.ValueChanged += args =>
        {
            if (Profile is null)
                return;

            if (!CanAffordPointBuy(abilityId, args.Value))
            {
                spin.OverrideValue(DisplayScore(abilityId, DndAbilityGeneration.PointBuy));
                return;
            }

            var abilityIds = AbilityIds();
            var scores = new Dictionary<string, int>(abilityIds.Count);
            foreach (var id in abilityIds)
                scores[id] = DisplayScore(id, DndAbilityGeneration.PointBuy);

            scores[abilityId] = args.Value;
            Profile = Profile.WithAbilityScores(ToScoreMap(scores));
            var updated = DisplayScore(abilityId, DndAbilityGeneration.PointBuy);
            summary.Text = FormatAbilitySummary(abilityId, updated);
            cost.Text = PointBuyCostText(updated);
            UpdateAbilityMethodSummary(DndAbilityGeneration.PointBuy);
            UpdateDerivedStats();
            SetDirty();
        };

        row.AddChild(spin);
        row.AddChild(cost);
        return row;
    }

    private OptionButton BuildStandardArrayPicker(string abilityId, int score)
    {
        var options = new OptionButton
        {
            HorizontalAlignment = HAlignment.Left,
            MinWidth = 80,
        };

        foreach (var arrayScore in DndAbilityScores.CopyStandardArray())
            options.AddItem(arrayScore.ToString(), arrayScore);

        options.SelectId(score);
        options.OnItemSelected += args =>
        {
            options.SelectId(args.Id);
            AssignStandardArrayScore(abilityId, args.Id);
        };

        return options;
    }

    private void AssignStandardArrayScore(string abilityId, int newScore)
    {
        if (Profile is null)
            return;

        var abilityIds = AbilityIds();
        var scores = ReadStoredScores(abilityIds);
        var normalized = DndAbilityScores.NormalizeStandardArray(abilityIds, scores);
        if (normalized.TryGetValue(abilityId, out var current) && current == newScore)
            return;

        string? otherId = null;
        foreach (var id in abilityIds)
        {
            if (id != abilityId && normalized[id] == newScore)
                otherId = id;
        }

        var previous = normalized[abilityId];
        normalized[abilityId] = newScore;
        if (otherId != null)
            normalized[otherId] = previous;

        Profile = Profile.WithAbilityScores(ToScoreMap(normalized));
        SetDirty();
        RefreshDndAbilities();
    }

    private bool CanAffordPointBuy(string abilityId, int newScore)
    {
        if (!DndAbilityScores.TryPointBuyCost(newScore, out _))
            return false;

        var abilityIds = AbilityIds();
        var scores = new Dictionary<string, int>(abilityIds.Count);
        foreach (var id in abilityIds)
            scores[id] = DisplayScore(id, DndAbilityGeneration.PointBuy);

        var before = DndAbilityScores.PointBuySpent(abilityIds, scores);
        scores[abilityId] = newScore;
        var after = DndAbilityScores.PointBuySpent(abilityIds, scores);
        return after <= DndAbilityScores.PointBuyBudget || after <= before;
    }

    private void UpdateAbilityMethodSummary(DndAbilityGeneration method)
    {
        var pointBuy = method == DndAbilityGeneration.PointBuy;
        DndPointBuyBudget.Visible = pointBuy;
        DndAbilityMethodHint.Text = Loc.GetString(pointBuy
            ? "dnd-ability-point-buy-hint"
            : "dnd-ability-standard-array-hint");

        if (!pointBuy)
            return;

        var abilityIds = AbilityIds();
        var scores = new Dictionary<string, int>(abilityIds.Count);
        foreach (var abilityId in abilityIds)
            scores[abilityId] = DisplayScore(abilityId, DndAbilityGeneration.PointBuy);

        var remaining = DndAbilityScores.PointBuyBudget - DndAbilityScores.PointBuySpent(abilityIds, scores);
        DndPointBuyBudget.Text = Loc.GetString(
            "dnd-ability-point-buy-budget",
            ("remaining", remaining),
            ("budget", DndAbilityScores.PointBuyBudget));
    }

    private string FormatAbilitySummary(string abilityId, int baseScore)
    {
        var score = FinalAbilityScore(abilityId, baseScore);
        var modifier = DndAbilityScores.FormatSigned(DndAbilityScores.Modifier(score));
        var band = Loc.GetString(DndAbilityScores.Band(score));
        var bonus = score - baseScore;
        if (bonus == 0)
        {
            return Loc.GetString(
                "dnd-ability-score-line",
                ("score", score),
                ("modifier", modifier),
                ("band", band));
        }

        return Loc.GetString(
            "dnd-ability-score-adjusted",
            ("base", baseScore),
            ("bonus", DndAbilityScores.FormatSigned(bonus)),
            ("score", score),
            ("modifier", modifier),
            ("band", band));
    }

    private static string PointBuyCostText(int score)
    {
        DndAbilityScores.TryPointBuyCost(score, out var cost);
        return Loc.GetString("dnd-ability-point-buy-cost", ("cost", cost));
    }

    private void UpdateDerivedStats()
    {
        var method = Profile?.AbilityGeneration ?? DndAbilityGeneration.PointBuy;
        var dexterity = FinalAbilityScore(DndAbilityPrototype.DexterityId, DisplayScore(DndAbilityPrototype.DexterityId, method));
        var level = DndAbilityScores.StartingLevel;
        var proficiency = DndAbilityScores.ProficiencyBonus(level);
        var constitution = FinalAbilityScore(DndAbilityPrototype.ConstitutionId, DisplayScore(DndAbilityPrototype.ConstitutionId, method));
        var classId = Profile?.DndClass.Id ?? DndClassPrototype.Default;
        if (!_prototypeManager.TryIndex<DndClassPrototype>(classId, out var dndClass))
        {
            classId = DndClassPrototype.Default;
            _prototypeManager.TryIndex<DndClassPrototype>(classId, out dndClass);
        }

        var unarmored = DndClassFeatures.Has(dndClass, _prototypeManager, DndFeatureKind.UnarmoredDefense, level);
        var armorClass = unarmored
            ? DndRageRules.UnarmoredArmorClass(dexterity, constitution)
            : DndAbilityScores.BaseArmorClass(dexterity);

        DndProficiencyLabel.Text = Loc.GetString(
            "dnd-ability-proficiency",
            ("bonus", DndAbilityScores.FormatSigned(proficiency)));
        DndProficiencyHint.Text = Loc.GetString("dnd-ability-proficiency-hint", ("level", level));
        DndArmorClassLabel.Text = Loc.GetString("dnd-ability-armor-class", ("ac", armorClass));
        if (unarmored)
        {
            DndArmorClassHint.Text = Loc.GetString(
                "dnd-ability-armor-class-unarmored-hint",
                ("dexterity", DndAbilityScores.FormatSigned(DndAbilityScores.Modifier(dexterity))),
                ("constitution", DndAbilityScores.FormatSigned(DndAbilityScores.Modifier(constitution))));
        }
        else
        {
            DndArmorClassHint.Text = Loc.GetString(
                "dnd-ability-armor-class-hint",
                ("modifier", DndAbilityScores.FormatSigned(DndAbilityScores.Modifier(dexterity))));
        }

        if (dndClass?.SpawnAsGhost == true)
        {
            DndHitPointsLabel.Text = Loc.GetString("dnd-hit-points-npc");
            DndHitPointsHint.Text = string.Empty;
            return;
        }

        var hitDie = dndClass?.HitDie ?? 10;
        var hitPoints = DndCombat.HitPoints(hitDie, constitution, level);
        DndHitPointsLabel.Text = Loc.GetString("dnd-hit-points", ("hp", hitPoints), ("die", hitDie));
        DndHitPointsHint.Text = Loc.GetString(
            "dnd-hit-points-hint",
            ("modifier", DndAbilityScores.FormatSigned(DndAbilityScores.Modifier(constitution))));
    }

    private int DisplayScore(string abilityId, DndAbilityGeneration method)
    {
        var stored = Profile?.GetAbilityScore(abilityId, method == DndAbilityGeneration.PointBuy
            ? DndAbilityScores.PointBuyMinimum
            : 0) ?? (method == DndAbilityGeneration.PointBuy ? DndAbilityScores.PointBuyMinimum : 0);

        if (method == DndAbilityGeneration.PointBuy)
        {
            if (DndAbilityScores.TryPointBuyCost(stored, out _))
                return stored;

            return Math.Clamp(stored, DndAbilityScores.PointBuyMinimum, DndAbilityScores.PointBuyMaximum);
        }

        var normalized = DndAbilityScores.NormalizeStandardArray(AbilityIds(), ReadStoredScores(AbilityIds()));
        return normalized.TryGetValue(abilityId, out var score) ? score : DndAbilityScores.PointBuyMinimum;
    }

    private List<string> AbilityIds()
    {
        return DndAbilityScores.OrderedIds(_prototypeManager.EnumeratePrototypes<DndAbilityPrototype>());
    }

    private Dictionary<string, int> ReadStoredScores(List<string> abilityIds)
    {
        var scores = new Dictionary<string, int>(abilityIds.Count);
        if (Profile is null)
            return scores;

        foreach (var abilityId in abilityIds)
        {
            if (Profile.AbilityScores.TryGetValue(abilityId, out var score))
                scores[abilityId] = score;
        }

        return scores;
    }

    private static Dictionary<ProtoId<DndAbilityPrototype>, int> ToScoreMap(Dictionary<string, int> scores)
    {
        var map = new Dictionary<ProtoId<DndAbilityPrototype>, int>(scores.Count);
        foreach (var (abilityId, score) in scores)
            map[abilityId] = score;

        return map;
    }
}
