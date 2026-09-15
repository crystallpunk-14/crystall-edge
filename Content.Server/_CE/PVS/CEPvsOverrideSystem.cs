/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Robust.Server.GameStates;
using Robust.Shared.Analyzers;

namespace Content.Server._CE.PVS;

public sealed partial class CEPvsOverrideSystem : EntitySystem
{
    [Dependency] private PvsOverrideSystem _pvs = default!;

    [SubscribeLocalEvent]
    private void OnPvsShutdown(Entity<CEPvsOverrideComponent> ent, ref ComponentShutdown args)
    {
        _pvs.RemoveGlobalOverride(ent);
    }

    [SubscribeLocalEvent]
    private void OnPvsStartup(Entity<CEPvsOverrideComponent> ent, ref ComponentStartup args)
    {
        _pvs.AddGlobalOverride(ent);
    }
}
