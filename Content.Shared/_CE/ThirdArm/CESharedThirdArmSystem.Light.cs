using Content.Shared._CE.ThirdArm.Components;

namespace Content.Shared._CE.ThirdArm;

public abstract partial class CESharedThirdArmSystem
{
    [Dependency] protected SharedPointLightSystem PointLight = default!;

    private void InitLight()
    {
        SubscribeLocalEvent<CEThirdArmModuleLightComponent, CEThirdArmModulePoweredEvent>(OnModuleLightPowered);
        SubscribeLocalEvent<CEThirdArmModuleLightComponent, CEThirdArmModuleUnpoweredEvent>(OnModuleLightUnpowered);
    }

    private void OnModuleLightPowered(Entity<CEThirdArmModuleLightComponent> ent, ref CEThirdArmModulePoweredEvent args)
    {
        PointLight.SetEnabled(ent, true);
    }

    private void OnModuleLightUnpowered(Entity<CEThirdArmModuleLightComponent> ent, ref CEThirdArmModuleUnpoweredEvent args)
    {
        PointLight.SetEnabled(ent, false);
    }
}
