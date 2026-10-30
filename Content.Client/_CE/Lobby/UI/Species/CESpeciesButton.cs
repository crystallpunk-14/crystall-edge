using System.Numerics;
using Content.Shared.Humanoid.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Lobby.UI.Species;

/// <summary>
/// A single square species tile in the lobby species picker: just the species icon (swapped for the
/// hovered icon while hovered or picked), with a green frame when picked and a black silhouette while locked.
/// </summary>
public sealed class CESpeciesButton : ContainerButton
{
    private static readonly Color FrameIdle = Color.FromHex("#3B3E56");
    private static readonly Color FrameHover = Color.FromHex("#9FA4C9");
    private static readonly Color FrameSelected = Color.FromHex("#4CC35A");
    private static readonly Color FrameSelectedHover = Color.FromHex("#8BF097");
    private static readonly Color Background = Color.FromHex("#1B1B23");
    private static readonly Color BackgroundHover = Color.FromHex("#2A2B3A");
    private static readonly Color BackgroundSelected = Color.FromHex("#1F3324");

    public readonly ProtoId<SpeciesPrototype> Species;

    private readonly PanelContainer _frame;
    private readonly StyleBoxFlat _frameStyle = new() { BorderThickness = new Thickness(2) };
    private readonly TextureRect _icon;
    private readonly Texture? _idleIcon;
    private readonly Texture? _hoveredIcon;

    private bool _selected;
    private bool _locked;
    private bool _hovered;

    public CESpeciesButton(ProtoId<SpeciesPrototype> species, Texture? icon, Texture? hoveredIcon)
    {
        Species = species;
        _idleIcon = icon;
        _hoveredIcon = hoveredIcon ?? icon;
        SetSize = new Vector2(104, 104);
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
            MinSize = new Vector2(96, 96),
            MouseFilter = MouseFilterMode.Ignore,
        };
        _frame.AddChild(_icon);

        UpdateVisuals();
    }

    public void SetState(bool selected, bool locked)
    {
        _selected = selected;
        _locked = locked;

        // A locked species that is still picked (e.g. the unlock was revoked) stays visible as picked.
        Disabled = !selected && locked;
        _icon.Modulate = locked ? Color.Black : Color.White;

        UpdateVisuals();
    }

    // Tracked by hand rather than via DrawMode: disabled (locked) tiles still react to hovering.
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
        _icon.Texture = _selected || _hovered ? _hoveredIcon : _idleIcon;

        if (_selected)
        {
            _frameStyle.BorderColor = _hovered ? FrameSelectedHover : FrameSelected;
            _frameStyle.BackgroundColor = BackgroundSelected;
            _frame.Modulate = Color.White;
            return;
        }

        _frameStyle.BorderColor = _hovered ? FrameHover : FrameIdle;
        _frameStyle.BackgroundColor = _hovered ? BackgroundHover : Background;
        _frame.Modulate = Color.White.WithAlpha(_locked ? 0.6f : _hovered ? 1f : 0.8f);
    }
}
