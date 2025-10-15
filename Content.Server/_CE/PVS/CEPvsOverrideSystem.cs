using Robust.Server.GameStates;

namespace Content.Server._CE.PVS;

public sealed partial class CEPvsOverrideSystem : EntitySystem
{
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<CEPvsOverrideComponent, ComponentStartup>(OnLighthouseStartup);
        SubscribeLocalEvent<CEPvsOverrideComponent, ComponentShutdown>(OnLighthouseShutdown);
    }

    private void OnLighthouseShutdown(Entity<CEPvsOverrideComponent> ent, ref ComponentShutdown args)
    {
        _pvs.RemoveGlobalOverride(ent);
    }

    private void OnLighthouseStartup(Entity<CEPvsOverrideComponent> ent, ref ComponentStartup args)
    {
        _pvs.AddGlobalOverride(ent);
    }
}
