using Content.Shared._CE.ThirdArm.Components;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.ThirdArm.LightModule;

public sealed partial class CEThirdArmModuleLightSystem : EntitySystem
{
    [Dependency] private SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    [SubscribeLocalEvent]
    private void OnModuleLightPowered(Entity<CEThirdArmLightModuleComponent> ent, ref CEThirdArmModulePoweredEvent args)
    {
        _light.SetEnabled(ent, true);
    }

    [SubscribeLocalEvent]
    private void OnModuleLightUnpowered(Entity<CEThirdArmLightModuleComponent> ent, ref CEThirdArmModuleUnpoweredEvent args)
    {
        _light.SetEnabled(ent, false);
    }
}
