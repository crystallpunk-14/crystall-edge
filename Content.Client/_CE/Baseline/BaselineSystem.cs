using Content.Shared.CCVar;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Client._CE.Baseline;

/// <summary>
/// On the client side, it automatically enables entity filtering to hide all vanilla ss14 entities
/// not marked with the ForkFiltered category from the spawn menu.
/// </summary>
public sealed class BaselineSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        _cfg.SetCVar(CVars.EntitiesCategoryFilter, "ForkFiltered");

        _cfg.OnValueChanged(CCVars.ServerLanguage, OnLanguageChange, true);
        _cfg.SetCVar(CVars.LocCultureName, _cfg.GetCVar(CCVars.ServerLanguage));
    }

    private void OnLanguageChange(string obj)
    {
        _cfg.SetCVar(CVars.LocCultureName, obj);
    }
}
