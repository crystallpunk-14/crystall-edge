using Content.Client.Overlays;
using Content.Shared._CE.MagicEssence.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client._CE.MagicEssence;

/// <summary>
/// Owns the lifecycle of <see cref="CEMagicEssenceOverlay"/>: adds/removes it whenever the
/// wearer has a <see cref="CEMagicEssenceScannerComponent"/> equipped (thaumaturgy glasses).
/// </summary>
public sealed partial class CEMagicEssenceOverlaySystem : EquipmentHudSystem<CEMagicEssenceScannerComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private CEMagicEssenceOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new CEMagicEssenceOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<CEMagicEssenceScannerComponent> component)
    {
        base.UpdateInternal(component);

        if (!_overlayMan.HasOverlay<CEMagicEssenceOverlay>())
            _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
