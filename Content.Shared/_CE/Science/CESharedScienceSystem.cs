using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.Science.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Science;

public abstract partial class CESharedScienceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeAchievement();
        InitializePen();
        InitializeScientificInterest();
    }

    /// <summary>
    /// Whether <paramref name="points"/> holds at least as much of every essence type in
    /// <paramref name="cost"/> as it demands. Essence types missing from <paramref name="points"/>
    /// are treated as 0. Pure comparison, usable client-side (e.g. for UI affordability checks) as
    /// well as server-side.
    /// </summary>
    public static bool CanAfford(
        IReadOnlyDictionary<ProtoId<CEMagicEssenceTypePrototype>, int> points,
        IReadOnlyDictionary<ProtoId<CEMagicEssenceTypePrototype>, int> cost)
    {
        foreach (var (essence, amount) in cost)
        {
            if (!points.TryGetValue(essence, out var have) || have < amount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Whether the given entity's research data holds enough of every essence type in
    /// <paramref name="cost"/>. Does not mutate anything.
    /// </summary>
    public bool HasEnoughPoints(Entity<CEScienceResearchDataComponent?> ent, IReadOnlyDictionary<ProtoId<CEMagicEssenceTypePrototype>, int> cost)
    {
        return Resolve(ent, ref ent.Comp, false) && CanAfford(ent.Comp.Points, cost);
    }

    /// <summary>
    /// Attempts to spend research points (one or more essence types) from the given entity's
    /// research data. Returns false (and does not mutate anything) if it doesn't have enough of
    /// any of them.
    /// </summary>
    public bool TrySpendPoints(Entity<CEScienceResearchDataComponent?> ent, IReadOnlyDictionary<ProtoId<CEMagicEssenceTypePrototype>, int> cost)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !CanAfford(ent.Comp.Points, cost))
            return false;

        foreach (var (essence, amount) in cost)
        {
            var remaining = ent.Comp.Points[essence] - amount;

            // Drop exhausted essence types entirely, rather than leaving a 0 entry around - the
            // UI enumerates this dictionary directly to decide what to render.
            if (remaining <= 0)
                ent.Comp.Points.Remove(essence);
            else
                ent.Comp.Points[essence] = remaining;
        }

        Dirty(ent.Owner, ent.Comp);
        return true;
    }

    /// <summary>
    /// Grants research points (one or more essence types) to the given entity's research data.
    /// </summary>
    public void GrantPoints(Entity<CEScienceResearchDataComponent?> ent, IReadOnlyDictionary<ProtoId<CEMagicEssenceTypePrototype>, int> amounts)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var (essence, amount) in amounts)
        {
            ent.Comp.Points[essence] = ent.Comp.Points.GetValueOrDefault(essence) + amount;
        }

        Dirty(ent.Owner, ent.Comp);
    }
}
