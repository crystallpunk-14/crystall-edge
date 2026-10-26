using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Classes;

public static class DndClassFeatures
{
    public static bool Has(DndClassPrototype? dndClass, IPrototypeManager prototypes, DndFeatureKind kind, int level)
    {
        if (dndClass == null)
            return false;

        foreach (var featureId in dndClass.Features)
        {
            if (!prototypes.TryIndex(featureId, out DndFeaturePrototype? feature))
                continue;

            if (feature.Kind == kind && feature.Level <= level)
                return true;
        }

        return false;
    }

    public static int MasteryCount(DndClassPrototype? dndClass, IPrototypeManager prototypes, int level)
    {
        if (dndClass == null)
            return 0;

        foreach (var featureId in dndClass.Features)
        {
            if (!prototypes.TryIndex(featureId, out DndFeaturePrototype? feature))
                continue;

            if (feature.Kind != DndFeatureKind.WeaponMastery || feature.Level > level)
                continue;

            return DndClassTables.AtLevel(feature.MasteryByLevel, level, 0);
        }

        return 0;
    }
}
