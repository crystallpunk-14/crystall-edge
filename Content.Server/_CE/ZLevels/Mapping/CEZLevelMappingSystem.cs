/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Server._CE.ZLevels.Core;
using Content.Shared._CE.ZLevels.Core.Components;

namespace Content.Server._CE.ZLevels.Mapping;

public sealed class CEZLevelMappingSystem : EntitySystem
{
    [Dependency] private readonly CEZLevelsSystem _zLevels = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZLevelMapComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CEZLevelMapComponent> ent, ref MapInitEvent args)
    {
        Log.Error("OnMapInit CEZLevelMapComponent ");
        if (!_zLevels.TryZNetwork((ent, ent.Comp), out var network))
            return;

        Log.Error("Adding components");
        EntityManager.AddComponents(ent, network.Value.Comp.Components);
    }
}
