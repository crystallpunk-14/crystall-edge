using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Controls;
using Content.Client.Guidebook.Richtext;
using Content.Shared._CE.MagicEssence.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Guidebook.Controls;

/// <summary>
/// Lists every thaumaturgical aspect grouped by tier, each rendered as a <see cref="CEGuideEssenceRecipeEmbed"/>
/// card. Sourced entirely from <see cref="CEMagicEssenceTypePrototype"/>, so adding or re-tiering an
/// aspect updates this guidebook page automatically.
/// </summary>
[UsedImplicitly]
public sealed partial class CEGuideEssenceRecipeGroupEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private IPrototypeManager _prototype = default!;

    public CEGuideEssenceRecipeGroupEmbed()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Stop;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        CreateEntries();
        control = this;
        return true;
    }

    private void CreateEntries()
    {
        var tiers = _prototype.EnumeratePrototypes<CEMagicEssenceTypePrototype>()
            .GroupBy(p => p.Tier)
            .OrderBy(g => g.Key);

        foreach (var tier in tiers)
        {
            var header = new Label
            {
                Text = tier.Key == 0
                    ? Loc.GetString("ce-guidebook-thaum-tier-basic")
                    : Loc.GetString("ce-guidebook-thaum-tier-header", ("tier", tier.Key)),
                StyleClasses = { "LabelHeading" },
            };

            var section = new TierSection(header);
            foreach (var essence in tier.OrderBy(p => p.Name))
            {
                section.AddCard(new CEGuideEssenceRecipeEmbed(essence));
            }

            AddChild(section);
        }
    }

    private sealed class TierSection : BoxContainer, ISearchableControl
    {
        private const int Columns = 3;
        private const float CardWidth = 200f;

        private readonly List<CEGuideEssenceRecipeEmbed> _cards = new();
        private readonly Control _header;
        private readonly GridContainer _grid;

        public TierSection(Control header)
        {
            Orientation = LayoutOrientation.Vertical;
            _header = header;
            AddChild(header);

            _grid = new GridContainer { Columns = Columns, HorizontalExpand = true };
            AddChild(_grid);
        }

        public void AddCard(CEGuideEssenceRecipeEmbed card)
        {
            card.HorizontalExpand = true;
            card.SetWidth = CardWidth;
            _cards.Add(card);
            _grid.AddChild(card);
        }

        public bool CheckMatchesSearch(string query)
        {
            return _cards.Any(card => card.CheckMatchesSearch(query));
        }

        public void SetHiddenState(bool state, string query)
        {
            var anyVisible = false;
            foreach (var card in _cards)
            {
                card.SetHiddenState(state, query);
                anyVisible |= card.Visible;
            }

            _header.Visible = anyVisible;
        }
    }
}
