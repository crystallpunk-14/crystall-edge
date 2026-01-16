using Content.Client.Items;
using Content.Client.Light.Components;
using Content.Client.Light.EntitySystems;
using Content.Client.Stylesheets;
using Content.Shared._CE.Power;
using Content.Shared._CE.Power.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._CE.Power;

public sealed class CEClientPowerSystem : VisualizerSystem<CEEnergyLeakComponent>
{
    [Dependency] private readonly LightBehaviorSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<CEEnergyTransferGloveComponent>(ent => new CEManaGloveStatusControl(ent));
    }

    protected override void OnAppearanceChange(EntityUid uid, CEEnergyLeakComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!AppearanceSystem.TryGetData<bool>(uid, CEPowerConsumerVisuals.Active, out var enabled))
            return;

        if (!TryComp<LightBehaviourComponent>(uid, out var beh))
            return;

        if (component.CurrentLeak > 0)
        {
            _light.StartLightBehaviour((uid, beh));
        }
        else
        {
            _light.StopLightBehaviour((uid, beh));
        }
    }
}


public sealed class CEManaGloveStatusControl : Control
{
    private readonly Entity<CEEnergyTransferGloveComponent> _parent;
    private readonly RichTextLabel _label;
    public CEManaGloveStatusControl(Entity<CEEnergyTransferGloveComponent> parent)
    {
        _parent = parent;

        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _label.Text = Loc.GetString("ce-energy-transfer-glove-status",
            ("mode",
                Loc.GetString(_parent.Comp.ConsumeMode
                    ? "ce-energy-transfer-glove-mode-drain"
                    : "ce-energy-transfer-glove-mode-transfer")));
    }
}
