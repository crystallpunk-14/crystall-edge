using Robust.Shared.Prototypes;

namespace Content.Shared._DND.Factions;

public static class DndSides
{
    public static readonly ProtoId<DndSidePrototype> Adventurers = "Adventurers";
    public static readonly ProtoId<DndSidePrototype> Goblins = "Goblins";
    public static readonly ProtoId<DndSidePrototype> Wolves = "Wolves";
    public static readonly ProtoId<DndSidePrototype> Neutral = "Neutral";
}

public enum DndStance
{
    Party,
    Neutral,
    Opposition,
}

/// <summary>
/// Party members share a side. Neutral starts no quarrel. Everyone else is opposition.
/// </summary>
public static class DndAllegianceRules
{
    public static ProtoId<DndSidePrototype>? Current(ProtoId<DndSidePrototype>? home, ProtoId<DndSidePrototype>? shift)
    {
        return shift ?? home;
    }

    public static DndStance Between(ProtoId<DndSidePrototype>? attacker, ProtoId<DndSidePrototype>? target)
    {
        if (attacker == null || target == null)
            return DndStance.Opposition;

        if (attacker == DndSides.Neutral || target == DndSides.Neutral)
            return DndStance.Neutral;

        if (attacker == target)
            return DndStance.Party;

        return DndStance.Opposition;
    }

    public static bool CanHarm(ProtoId<DndSidePrototype>? attacker, ProtoId<DndSidePrototype>? target)
    {
        return Between(attacker, target) != DndStance.Party;
    }
}
