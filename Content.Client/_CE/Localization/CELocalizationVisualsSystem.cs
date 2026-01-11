using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Client._CE.Localization;

public sealed class CELocalizationVisualsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CELocalizationVisualsComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(Entity<CELocalizationVisualsComponent> visuals, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(visuals, out var sprite))
            return;

        foreach (var (map, pDictionary) in visuals.Comp.MapStates)
        {
            if (!pDictionary.TryGetValue(_cfg.GetCVar(CCVars.ServerLanguage), out var state))
                return;

            if (_sprite.LayerMapTryGet((visuals.Owner, sprite), map, out _, false))
                _sprite.LayerSetRsiState((visuals.Owner, sprite), map, state);
        }
    }
}
