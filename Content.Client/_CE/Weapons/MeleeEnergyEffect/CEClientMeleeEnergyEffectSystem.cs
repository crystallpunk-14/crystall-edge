using Content.Client.Items;
using Content.Client.Stylesheets;
using Content.Client.Weapons.Ranged.ItemStatus;
using Content.Shared._CE.Weapons.MeleeEnergyEffect;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._CE.Weapons.MeleeEnergyEffect;

public sealed class CEClientMeleeEnergyEffectSystem : CESharedMeleeEnergyEffectSystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<CEMeleeEnergyEffectComponent>(ent => new CEMeleeEnergyEffectStatusControl(ent));
    }
}

public sealed class CEMeleeEnergyEffectStatusControl : Control
{
    private readonly Entity<CEMeleeEnergyEffectComponent> _parent;
    private readonly BatteryBulletRenderer _bullets;
    private readonly Label _ammoCount;

    public CEMeleeEnergyEffectStatusControl(Entity<CEMeleeEnergyEffectComponent> parent)
    {
        _parent = parent;
        MinHeight = 15;
        HorizontalExpand = true;
        VerticalAlignment = VAlignment.Center;

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                (_bullets = new BatteryBulletRenderer
                {
                    Margin = new Thickness(0, 0, 5, 0),
                    HorizontalExpand = true
                }),
                (_ammoCount = new Label
                {
                    StyleClasses = { StyleClass.ItemStatus },
                    HorizontalAlignment = HAlignment.Right,
                    VerticalAlignment = VAlignment.Bottom
                }),
            }
        });
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        _ammoCount.Visible = true;

        _ammoCount.Text = $"x{ _parent.Comp.Hits:00}";

        _bullets.Capacity = _parent.Comp.Capacity;
        _bullets.Count = _parent.Comp.Hits;
    }
}
