using System.Numerics;
using Content.Shared._CE.Traits;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Traits.UI;

/// <summary>
/// A single trait tile in the lobby trait grid: the skill icon with a cost badge, drawn translucent
/// until picked, with a green frame when picked and a black silhouette while locked.
/// </summary>
public sealed class CETraitButton : ContainerButton
{
    private static readonly Color FrameIdle = Color.FromHex("#3B3E56");
    private static readonly Color FrameHover = Color.FromHex("#9FA4C9");
    private static readonly Color FrameSelected = Color.FromHex("#4CC35A");
    private static readonly Color FrameSelectedHover = Color.FromHex("#8BF097");
    private static readonly Color FrameBlocked = Color.FromHex("#6E2B2B");
    private static readonly Color Background = Color.FromHex("#1B1B23");
    private static readonly Color BackgroundHover = Color.FromHex("#2A2B3A");
    private static readonly Color BackgroundSelected = Color.FromHex("#1F3324");

    public readonly ProtoId<CETraitPrototype> Trait;

    private readonly PanelContainer _frame;
    private readonly StyleBoxFlat _frameStyle = new() { BorderThickness = new Thickness(2) };
    private readonly TextureRect _icon;
    private readonly PanelContainer _costBadge;

    private bool _selected;
    private bool _locked;
    private bool _blocked;
    private bool _hovered;

    public CETraitButton(ProtoId<CETraitPrototype> trait, Texture? icon, int cost)
    {
        Trait = trait;
        MinSize = new Vector2(72, 72);
        Margin = new Thickness(3);

        _frame = new PanelContainer { PanelOverride = _frameStyle };
        AddChild(_frame);

        _icon = new TextureRect
        {
            Texture = icon,
            TextureScale = new Vector2(2, 2),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            MouseFilter = MouseFilterMode.Ignore,
        };
        _frame.AddChild(_icon);

        _costBadge = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = CostColor(cost).WithAlpha(0.85f) },
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Bottom,
            Margin = new Thickness(0, 0, 3, 3),
            MouseFilter = MouseFilterMode.Ignore,
        };
        _costBadge.AddChild(new Label
        {
            Text = FormatCost(cost),
            FontColorOverride = Color.White,
            Margin = new Thickness(4, 0),
        });
        _frame.AddChild(_costBadge);

        UpdateVisuals();
    }

    public void SetState(bool selected, bool locked, bool blocked)
    {
        _selected = selected;
        _locked = locked;
        _blocked = blocked;

        // A locked trait that is still picked (e.g. the unlock was revoked) stays clickable so it can be unpicked.
        Disabled = !selected && (locked || blocked);
        _costBadge.Visible = !locked;
        _icon.Modulate = locked ? Color.Black : Color.White;

        UpdateVisuals();
    }

    public static string FormatCost(int cost)
    {
        return cost > 0 ? $"+{cost}" : cost.ToString();
    }

    public static Color CostColor(int cost)
    {
        return cost switch
        {
            > 0 => Color.FromHex("#3E9E4A"),
            < 0 => Color.FromHex("#B0413E"),
            _ => Color.FromHex("#6B6B7A"),
        };
    }

    // Tracked by hand rather than via DrawMode: disabled (locked/blocked) tiles still react to hovering.
    protected override void MouseEntered()
    {
        base.MouseEntered();
        _hovered = true;
        UpdateVisuals();
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _hovered = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        var hovered = _hovered;

        if (_selected)
        {
            _frameStyle.BorderColor = hovered ? FrameSelectedHover : FrameSelected;
            _frameStyle.BackgroundColor = BackgroundSelected;
            _frame.Modulate = Color.White;
            return;
        }

        if (_blocked && !_locked)
            _frameStyle.BorderColor = FrameBlocked;
        else
            _frameStyle.BorderColor = hovered ? FrameHover : FrameIdle;

        _frameStyle.BackgroundColor = hovered ? BackgroundHover : Background;

        // Unpicked traits are translucent; hovering brings them most of the way in as feedback.
        float alpha;
        if (_locked)
            alpha = hovered ? 1f : 0.85f;
        else if (_blocked)
            alpha = hovered ? 0.4f : 0.25f;
        else
            alpha = hovered ? 0.85f : 0.45f;

        _frame.Modulate = Color.White.WithAlpha(alpha);
    }
}
