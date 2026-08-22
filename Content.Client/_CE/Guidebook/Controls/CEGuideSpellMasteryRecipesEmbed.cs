using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client._CE.MagicEssence.Controls;
using Content.Client._CE.SpellMastery;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Guidebook.Richtext;
using Content.Client.Message;
using Content.Shared._CE.ResourceManager;
using Content.Shared._CE.Skill;
using Content.Shared._CE.Skill.Prototypes;
using Content.Shared._CE.SpellMastery;
using Content.Shared._CE.SpellMastery.Prototypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Guidebook.Controls;

// CrystallEdge: rough copy of CEGuideInfusionAltarRecipesEmbed/CEGuideInfusionAltarRecipeEmbed, built
// in plain C# instead of XAML - not yet cleaned up/unified. The diagram mirrors the infusion altar's
// (altar in the center, pedestals orbiting), but instead of a catalyst item floating above the altar,
// draws a CEMobHuman rotated on its side, since the "catalyst" here is a buckled player. The result is
// a skill rather than an item, so its icon/name/description come from CESharedSkillSystem instead of
// indexing an EntProtoId directly.

/// <summary>
/// Lists the spell mastery ritual recipes currently known to the local player. Recipe knowledge is
/// per-player hidden server state, so this asks the server fresh via
/// <see cref="CESpellMasteryKnownRecipesSystem"/> every time the page is opened instead of reading any
/// locally cached data.
/// </summary>
[UsedImplicitly]
public sealed partial class CEGuideSpellMasteryRecipesEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IEntityManager _entity = default!;

    private static readonly EntProtoId AltarProto = "CESpellMasteryAltar";
    private static readonly EntProtoId PlayerProto = "CEMobHuman";
    private static readonly EntProtoId PedestalProto = "CEInfusionAltarPedestal";

    private const float DiagramWidth = 280f;
    private const float DiagramHeight = 260f;
    private const float PedestalOrbitRadius = 90f;
    private const float AltarIconSize = 80f;
    private const float PlayerIconSize = 56f;
    private const float PedestalIconSize = 80f;
    private const float PedestalItemIconSize = 60f;
    private const float EssenceIconSize = 28f;
    private const float SkillIconSize = 64f;

    private readonly CESpellMasteryKnownRecipesSystem _knownRecipes;
    private readonly CESharedSkillSystem _skill;
    private readonly SpriteSystem _sprite;
    private readonly Label _emptyLabel;

    public CEGuideSpellMasteryRecipesEmbed()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Stop;

        _knownRecipes = _entity.System<CESpellMasteryKnownRecipesSystem>();
        _skill = _entity.System<CESharedSkillSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _emptyLabel = new Label { Text = Loc.GetString("ce-guidebook-spell-mastery-none-known") };
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        _knownRecipes.OnRecipesUpdated += OnRecipesUpdated;
        _knownRecipes.RequestKnownRecipes();

        AddChild(_emptyLabel);

        control = this;
        return true;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _knownRecipes.OnRecipesUpdated -= OnRecipesUpdated;
    }

    private void OnRecipesUpdated(List<CESpellMasteryKnownRecipeInfo> recipes)
    {
        RemoveAllChildren();

        if (recipes.Count == 0)
        {
            AddChild(_emptyLabel);
            return;
        }

        foreach (var info in recipes)
        {
            if (!_prototype.TryIndex(info.Recipe, out CESpellMasteryRitualRecipePrototype? recipe))
                continue;

            AddChild(BuildRecipeCard(recipe, info));
        }
    }

    private Control BuildRecipeCard(CESpellMasteryRitualRecipePrototype recipe, CESpellMasteryKnownRecipeInfo info)
    {
        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Margin = new Thickness(10),
        };

        var skillColumn = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            MinWidth = 160,
            MaxWidth = 160,
        };
        skillColumn.AddChild(CreateSkillIcon(recipe.Result));
        skillColumn.AddChild(new Label
        {
            Text = _skill.GetSkillName(recipe.Result),
            StyleClasses = { "LabelHeadingBigger" },
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0),
        });

        var desc = _skill.GetSkillDescription(recipe.Result);
        if (!string.IsNullOrEmpty(desc))
        {
            var descLabel = new RichTextLabel { Margin = new Thickness(0, 6, 0, 0) };
            descLabel.SetMarkup(desc);
            skillColumn.AddChild(descLabel);
        }

        root.AddChild(skillColumn);
        root.AddChild(new VSeparator { Margin = new Thickness(10, 0, 10, 0), VerticalExpand = true });

        var rightColumn = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center,
        };

        rightColumn.AddChild(BuildDiagram(recipe, info));

        rightColumn.AddChild(new PanelContainer { StyleClasses = { "LowDivider" }, Margin = new Thickness(0, 6, 0, 6) });

        rightColumn.AddChild(new Label
        {
            Text = Loc.GetString("ce-guidebook-spell-mastery-essences"),
            Margin = new Thickness(0, 0, 0, 4),
            HorizontalAlignment = HAlignment.Center,
        });

        var essenceRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            SeparationOverride = 12,
        };
        foreach (var (type, amount) in info.Essences)
        {
            var essenceControl = new CEEssenceAmountControl { SetSize = new Vector2(EssenceIconSize, EssenceIconSize) };
            essenceControl.SetEssence(type, amount);
            essenceRow.AddChild(essenceControl);
        }
        rightColumn.AddChild(essenceRow);

        root.AddChild(rightColumn);

        var panel = new PanelContainer
        {
            Margin = new Thickness(0, 0, 0, 6),
            PanelOverride = new StyleBoxFlat
            {
                BorderThickness = new Thickness(1),
                BorderColor = Color.FromHex("#777777"),
                BackgroundColor = Color.FromHex("#202023"),
            },
        };
        panel.AddChild(root);

        return panel;
    }

    /// <summary>
    /// Draws the altar in the center with a lying CEMobHuman above it (in place of the infusion
    /// altar's floating catalyst item), and the recipe's pedestal items arranged evenly around it in a
    /// circle starting from the top - same layout algorithm as CEGuideInfusionAltarRecipeEmbed.
    /// </summary>
    private Control BuildDiagram(CESpellMasteryRitualRecipePrototype recipe, CESpellMasteryKnownRecipeInfo info)
    {
        var root = new LayoutContainer { MinSize = new Vector2(DiagramWidth, DiagramHeight) };
        var center = new Vector2(DiagramWidth / 2f, DiagramHeight / 2f);

        var groups = new List<(Control Entity, Vector2 Center, float EntitySize, Control? Item)>
        {
            (CreateEntityIcon(AltarProto, AltarIconSize), center, AltarIconSize, CreatePlayerIcon()),
        };

        var pedestalCount = info.PedestalRequirementIndices.Count;
        for (var i = 0; i < pedestalCount; i++)
        {
            var angle = -MathF.PI / 2f + i * (2f * MathF.PI / pedestalCount);
            var pedestalCenter = center + PedestalOrbitRadius * new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            Control? itemIcon = null;
            var index = info.PedestalRequirementIndices[i];
            if (index >= 0 && index < recipe.PedestalRequirementPool.Count)
            {
                var pedestalRequirement = recipe.PedestalRequirementPool[index].Requirement;
                itemIcon = CreateRequirementIcon(pedestalRequirement, PedestalItemIconSize);
            }

            groups.Add((CreateEntityIcon(PedestalProto, PedestalIconSize), pedestalCenter, PedestalIconSize, itemIcon));
        }

        foreach (var group in groups.OrderBy(g => g.Center.Y))
        {
            PlaceCentered(root, group.Entity, group.Center);
            if (group.Item is { } item)
                PlaceAboveEntity(root, item, group.Center, group.EntitySize);
        }

        return root;
    }

    private Control CreatePlayerIcon()
    {
        var view = new EntityPrototypeView
        {
            MinSize = new Vector2(PlayerIconSize, PlayerIconSize),
            Scale = EntityViewScale(PlayerIconSize),
            MouseFilter = MouseFilterMode.Stop,
            EyeRotation = Angle.FromDegrees(-90),
            ToolTip = _prototype.TryIndex(PlayerProto, out var protoData) ? protoData.Name : PlayerProto.Id,
        };
        view.SetPrototype(PlayerProto);
        return view;
    }

    private EntityPrototypeView CreateEntityIcon(EntProtoId proto, float size)
    {
        var view = new EntityPrototypeView
        {
            MinSize = new Vector2(size, size),
            Scale = EntityViewScale(size),
            MouseFilter = MouseFilterMode.Stop,
            ToolTip = _prototype.TryIndex(proto, out var protoData) ? protoData.Name : proto.Id,
            // Faded so the item/player resting on top (drawn separately, full opacity) reads as the focus.
            Modulate = Color.White.WithAlpha(0.3f),
        };
        view.SetPrototype(proto);
        return view;
    }

    private Control CreateRequirementIcon(CEResourceRequirement requirement, float size)
    {
        var tooltip = requirement.GetRequirementTitle(_prototype);

        if (requirement.GetRequirementTexture(_prototype) is { } texture)
        {
            return new TextureRect
            {
                Texture = _sprite.Frame0(texture),
                MinSize = new Vector2(size, size),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                MouseFilter = MouseFilterMode.Stop,
                ToolTip = tooltip,
            };
        }

        if (requirement.GetRequirementEntityView(_prototype) is { } entityView)
        {
            var view = new EntityPrototypeView
            {
                MinSize = new Vector2(size, size),
                Scale = EntityViewScale(size),
                MouseFilter = MouseFilterMode.Stop,
                ToolTip = tooltip,
            };
            view.SetPrototype(entityView);
            return view;
        }

        return new Control { MinSize = new Vector2(size, size) };
    }

    private Control CreateSkillIcon(ProtoId<CESkillPrototype> skill)
    {
        if (_skill.GetSkillPreviewEntity(skill) is { } preview)
        {
            var view = new EntityPrototypeView
            {
                MinSize = new Vector2(SkillIconSize, SkillIconSize),
                Scale = EntityViewScale(SkillIconSize),
                HorizontalAlignment = HAlignment.Center,
            };
            view.SetPrototype(preview.ID);
            return view;
        }

        if (_skill.GetSkillIcon(skill) is { } icon)
        {
            return new TextureRect
            {
                Texture = _sprite.Frame0(icon),
                MinSize = new Vector2(SkillIconSize, SkillIconSize),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                HorizontalAlignment = HAlignment.Center,
            };
        }

        return new Control { MinSize = new Vector2(SkillIconSize, SkillIconSize) };
    }

    private static Vector2 EntityViewScale(float targetSize)
    {
        var factor = targetSize / EyeManager.PixelsPerMeter;
        return new Vector2(factor, factor);
    }

    /// <summary>
    /// Positions a control so its own center sits at <paramref name="center"/>.
    /// </summary>
    private static void PlaceCentered(LayoutContainer root, Control control, Vector2 center)
    {
        root.AddChild(control);
        LayoutContainer.SetPosition(control, center - control.MinSize / 2f);
    }

    private static void PlaceAboveEntity(LayoutContainer root, Control control, Vector2 entityCenter, float entitySize)
    {
        PlaceCentered(root, control, entityCenter - new Vector2(0f, entitySize / 4f));
    }
}
