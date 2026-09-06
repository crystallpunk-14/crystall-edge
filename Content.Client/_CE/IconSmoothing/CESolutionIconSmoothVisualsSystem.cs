using Content.Client.IconSmoothing;
using Content.Shared._CE.IconSmoothing;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;

namespace Content.Client._CE.IconSmoothing;

/// <summary>
/// Selects an authored IconSmooth state family from the canonical solution appearance.
/// </summary>
public sealed partial class CESolutionIconSmoothVisualsSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private IconSmoothSystem _iconSmooth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CESolutionIconSmoothVisualsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CESolutionIconSmoothVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnMapInit(Entity<CESolutionIconSmoothVisualsComponent> ent, ref MapInitEvent args)
    {
        UpdateStateBase(ent);
    }

    private void OnAppearanceChange(
        Entity<CESolutionIconSmoothVisualsComponent> ent,
        ref AppearanceChangeEvent args)
    {
        UpdateStateBase(ent, args.Component);
    }

    private void UpdateStateBase(
        Entity<CESolutionIconSmoothVisualsComponent> ent,
        AppearanceComponent? appearance = null)
    {
        if (ent.Comp.StateBases.Count < 2
            || !Resolve(ent.Owner, ref appearance, false)
            || !TryComp<IconSmoothComponent>(ent, out var smooth)
            || !TryComp<SolutionContainerVisualsComponent>(ent, out var solutionVisuals)
            || !_appearance.TryGetData(
                ent.Owner,
                SolutionContainerVisuals.FillFraction,
                out float fraction,
                appearance))
        {
            return;
        }

        // A container may publish updates for several solutions. Use the same selection
        // contract as the canonical solution visualizer, which owns the appearance data.
        if (!string.IsNullOrEmpty(solutionVisuals.SolutionName)
            && _appearance.TryGetData(ent.Owner, SolutionContainerVisuals.SolutionName,
                out string solutionName, appearance)
            && solutionName != solutionVisuals.SolutionName)
        {
            return;
        }

        var level = ContentHelpers.RoundToLevels(
            Math.Clamp(fraction, 0f, 1f),
            1,
            ent.Comp.StateBases.Count);
        var stateBase = ent.Comp.StateBases[level];

        if (smooth.StateBase == stateBase)
            return;

        // SetStateBase recreates corner layers without removing the old ones.
        // Recalculate the existing layers so repeated level changes do not accumulate sprites.
        smooth.StateBase = stateBase;
        _iconSmooth.DirtyNeighbours(ent.Owner, smooth);
    }
}
