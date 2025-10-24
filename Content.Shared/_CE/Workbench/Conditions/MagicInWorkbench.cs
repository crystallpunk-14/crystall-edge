using Content.Shared._CE.MagicEnergy;
using Content.Shared._CE.MagicEnergy.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Workbench.Conditions;

public sealed partial class MagicInWorkbench : CEWorkbenchCraftCondition
{
    [DataField]
    public FixedPoint2 Energy = 10;

    public override bool CheckCondition(
        EntityManager entManager,
        IPrototypeManager protoManager,
        EntityUid workbench,
        EntityUid user)
    {
        if (!entManager.TryGetComponent<CEMagicEnergyContainerComponent>(workbench, out var energyContainer))
            return false;

        return energyContainer.Energy >= Energy;
    }

    public override void PostCraft(
        EntityManager entManager,
        IPrototypeManager protoManager,
        EntityUid workbench,
        EntityUid user)
    {
        var magicSys = entManager.System<CESharedMagicEnergySystem>();

        magicSys.ChangeEnergy(workbench, -Energy, out _, out _);
    }

    public override void FailedEffect(
        EntityManager entManager,
        IPrototypeManager protoManager,
        EntityUid workbench,
        EntityUid user)
    {
        var magicSys = entManager.System<CESharedMagicEnergySystem>();
        magicSys.ChangeEnergy(workbench, -Energy, out _, out _);

        if (entManager.TryGetComponent<TransformComponent>(workbench, out var xform))
            entManager.SpawnAtPosition("CESkyLightning", xform.Coordinates);
    }

    public override string GetConditionTitle(
        EntityManager entManager,
        IPrototypeManager protoManager,
        EntityUid workbench,
        EntityUid user)
    {
        if (!entManager.TryGetComponent<CEMagicEnergyContainerComponent>(workbench, out var energyContainer))
            return string.Empty;
        var manaProcent = Energy / energyContainer.MaxEnergy * 100;

        return Loc.GetString("cp14-workbench-condition-mana-in-w", ("count", manaProcent));
    }
}
