using Content.Shared._CE.Science.Components;

namespace Content.Shared._CE.Science;

public abstract partial class CESharedScienceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeAchievement();
    }

    /// <summary>
    /// Attempts to spend research points from the given entity's research data. Returns false
    /// (and does not mutate anything) if it doesn't have enough.
    /// </summary>
    public bool TrySpendPoints(Entity<CEScienceResearchDataComponent?> ent, int amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Points < amount)
            return false;

        ent.Comp.Points -= amount;
        Dirty(ent.Owner, ent.Comp);
        return true;
    }

    /// <summary>
    /// Grants research points to the given entity's research data.
    /// </summary>
    public void GrantPoints(Entity<CEScienceResearchDataComponent?> ent, int amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Points += amount;
        Dirty(ent.Owner, ent.Comp);
    }
}
