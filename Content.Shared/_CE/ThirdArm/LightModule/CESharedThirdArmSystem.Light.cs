using Content.Shared._CE.ThirdArm.Components;

namespace Content.Shared._CE.ThirdArm.LightModule;

public sealed partial class CEThirdArmLightModuleSystem : EntitySystem
{
    [Dependency] protected SharedPointLightSystem PointLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEThirdArmLightModuleComponent, CEThirdArmModulePoweredEvent>(OnModuleLightPowered);
        SubscribeLocalEvent<CEThirdArmLightModuleComponent, CEThirdArmModuleUnpoweredEvent>(OnModuleLightUnpowered);
    }

    private void OnModuleLightPowered(Entity<CEThirdArmLightModuleComponent> ent, ref CEThirdArmModulePoweredEvent args)
    {
        PointLight.SetEnabled(ent, true);
    }

    private void OnModuleLightUnpowered(Entity<CEThirdArmLightModuleComponent> ent, ref CEThirdArmModuleUnpoweredEvent args)
    {
        PointLight.SetEnabled(ent, false);
    }
}
