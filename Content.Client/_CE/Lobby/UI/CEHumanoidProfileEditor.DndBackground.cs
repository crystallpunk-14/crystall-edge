using Content.Client.Stylesheets;
using Content.Shared._DND.Abilities;
using Content.Shared._DND.Backgrounds;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._CE.Lobby.UI;

public sealed partial class CEHumanoidProfileEditor
{
    private void AddDndBackgroundSection()
    {
        var backgrounds = OrderedBackgrounds();
        if (backgrounds.Count == 0)
            return;

        DndAbilityList.AddChild(new Label
        {
            Text = Loc.GetString("dnd-background-header"),
            StyleClasses = { StyleClass.LabelHeading },
            Margin = new Thickness(0, 16, 0, 4),
        });
        DndAbilityList.AddChild(new Label
        {
            Text = Loc.GetString("dnd-background-hint"),
            FontColorOverride = Color.Gray,
            Margin = new Thickness(0, 0, 0, 6),
        });

        var selected = Profile?.DndBackground.Id ?? DndBackgroundPrototype.Default;
        var picker = new OptionButton { HorizontalExpand = true };
        var selectedIndex = 0;
        for (var i = 0; i < backgrounds.Count; i++)
        {
            picker.AddItem(Loc.GetString(backgrounds[i].Name), i);
            if (backgrounds[i].ID == selected)
                selectedIndex = i;
        }

        picker.SelectId(selectedIndex);
        picker.OnItemSelected += args =>
        {
            picker.SelectId(args.Id);
            SetDndBackground(backgrounds[args.Id]);
        };
        DndAbilityList.AddChild(picker);

        var background = backgrounds[selectedIndex];
        DndAbilityList.AddChild(new Label
        {
            Text = Loc.GetString(background.Description),
            FontColorOverride = Color.Gray,
            Margin = new Thickness(0, 4, 0, 2),
        });

        if (background.Abilities.Count >= 3)
        {
            DndAbilityList.AddChild(new Label
            {
                Text = Loc.GetString(
                    "dnd-background-abilities",
                    ("first", AbilityName(background.Abilities[0].Id)),
                    ("second", AbilityName(background.Abilities[1].Id)),
                    ("third", AbilityName(background.Abilities[2].Id))),
                Margin = new Thickness(0, 0, 0, 6),
            });
        }

        var increase = Profile?.BackgroundIncrease ?? DndBackgroundIncrease.AllThree;
        var modes = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            Margin = new Thickness(0, 0, 0, 6),
        };
        var group = new ButtonGroup(isNoneSetAllowed: false);
        var allThree = new Button
        {
            Text = Loc.GetString("dnd-background-increase-all"),
            ToggleMode = true,
            Group = group,
            Pressed = increase == DndBackgroundIncrease.AllThree,
        };
        var split = new Button
        {
            Text = Loc.GetString("dnd-background-increase-split"),
            ToggleMode = true,
            Group = group,
            Pressed = increase == DndBackgroundIncrease.Split,
        };
        allThree.OnPressed += _ => SetBackgroundIncrease(DndBackgroundIncrease.AllThree, background);
        split.OnPressed += _ => SetBackgroundIncrease(DndBackgroundIncrease.Split, background);
        modes.AddChild(allThree);
        modes.AddChild(split);
        DndAbilityList.AddChild(modes);

        if (increase == DndBackgroundIncrease.Split)
            AddSplitPickers(background);
    }

    private void AddSplitPickers(DndBackgroundPrototype background)
    {
        var plusTwo = Profile?.BackgroundPlusTwo.Id ?? background.Abilities[0].Id;
        var plusOne = Profile?.BackgroundPlusOne.Id ?? background.Abilities[1].Id;
        if (!DndBackgroundScores.IsValidSplit(background.Abilities, plusTwo, plusOne))
        {
            plusTwo = background.Abilities[0].Id;
            plusOne = background.Abilities[1].Id;
        }

        DndAbilityList.AddChild(BuildIncreasePicker("dnd-background-plus-two", background, plusTwo, true, plusOne));
        DndAbilityList.AddChild(BuildIncreasePicker("dnd-background-plus-one", background, plusOne, false, plusTwo));
    }

    private BoxContainer BuildIncreasePicker(
        string label,
        DndBackgroundPrototype background,
        string selectedId,
        bool isPlusTwo,
        string otherId)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            Margin = new Thickness(0, 0, 0, 4),
        };
        row.AddChild(new Label
        {
            Text = Loc.GetString(label),
            MinWidth = 120,
            VerticalAlignment = VAlignment.Center,
        });

        var options = new OptionButton { MinWidth = 160 };
        var selectedIndex = 0;
        for (var i = 0; i < background.Abilities.Count; i++)
        {
            var abilityId = background.Abilities[i].Id;
            options.AddItem(AbilityName(abilityId), i);
            if (abilityId == selectedId)
                selectedIndex = i;
        }

        options.SelectId(selectedIndex);
        options.OnItemSelected += args =>
        {
            options.SelectId(args.Id);
            var chosen = background.Abilities[args.Id].Id;
            var plusTwo = isPlusTwo ? chosen : otherId;
            var plusOne = isPlusTwo ? otherId : chosen;
            if (plusTwo == plusOne)
            {
                if (isPlusTwo)
                    plusOne = selectedId;
                else
                    plusTwo = selectedId;
            }

            SetBackgroundIncrease(DndBackgroundIncrease.Split, background, plusTwo, plusOne);
        };
        row.AddChild(options);
        return row;
    }

    private void SetDndBackground(DndBackgroundPrototype background)
    {
        if (Profile is null || Profile.DndBackground.Id == background.ID)
            return;

        Profile = Profile.WithDndBackground(background.ID);
        if (!DndBackgroundScores.IsValidSplit(background.Abilities, Profile.BackgroundPlusTwo, Profile.BackgroundPlusOne)
            && background.Abilities.Count >= 2)
        {
            Profile = Profile.WithBackgroundIncrease(
                Profile.BackgroundIncrease,
                background.Abilities[0],
                background.Abilities[1]);
        }

        SetDirty();
        RefreshDndAbilities();
    }

    private void SetBackgroundIncrease(DndBackgroundIncrease increase, DndBackgroundPrototype background)
    {
        var plusTwo = Profile?.BackgroundPlusTwo.Id ?? background.Abilities[0].Id;
        var plusOne = Profile?.BackgroundPlusOne.Id ?? background.Abilities[1].Id;
        SetBackgroundIncrease(increase, background, plusTwo, plusOne);
    }

    private void SetBackgroundIncrease(
        DndBackgroundIncrease increase,
        DndBackgroundPrototype background,
        string plusTwo,
        string plusOne)
    {
        if (Profile is null)
            return;

        if (!DndBackgroundScores.IsValidSplit(background.Abilities, plusTwo, plusOne) && background.Abilities.Count >= 2)
        {
            plusTwo = background.Abilities[0].Id;
            plusOne = background.Abilities[1].Id;
        }

        if (Profile.BackgroundIncrease == increase
            && Profile.BackgroundPlusTwo.Id == plusTwo
            && Profile.BackgroundPlusOne.Id == plusOne)
            return;

        Profile = Profile.WithBackgroundIncrease(increase, plusTwo, plusOne);
        SetDirty();
        RefreshDndAbilities();
    }

    private int FinalAbilityScore(string abilityId, int baseScore)
    {
        if (Profile is null || !_prototypeManager.TryIndex(Profile.DndBackground, out DndBackgroundPrototype? background))
            return baseScore;

        var bonus = DndBackgroundScores.Bonus(
            abilityId,
            background.Abilities,
            Profile.BackgroundIncrease,
            Profile.BackgroundPlusTwo.Id,
            Profile.BackgroundPlusOne.Id);
        return DndBackgroundScores.FinalScore(baseScore, bonus);
    }

    private string AbilityName(string abilityId)
    {
        if (_prototypeManager.TryIndex<DndAbilityPrototype>(abilityId, out var ability))
            return Loc.GetString(ability.Name);

        return abilityId;
    }

    private List<DndBackgroundPrototype> OrderedBackgrounds()
    {
        var list = new List<DndBackgroundPrototype>();
        foreach (var background in _prototypeManager.EnumeratePrototypes<DndBackgroundPrototype>())
            list.Add(background);

        list.Sort(static (a, b) =>
        {
            var order = a.Order.CompareTo(b.Order);
            return order != 0 ? order : string.CompareOrdinal(a.ID, b.ID);
        });
        return list;
    }
}
