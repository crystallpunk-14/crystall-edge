using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._DND.Spells;

/// <summary>
/// Bless adds 1d4 to each attack and save while it lasts. Guidance adds 1d4 once.
/// </summary>
public static class DndSpellBuffs
{
    public static int RollDie(EntityUid uid, IEntityManager entities, IGameTiming timing, IRobustRandom random)
    {
        var bonus = 0;
        if (entities.TryGetComponent(uid, out DndBlessComponent? bless) && timing.CurTime < bless.Until)
            bonus += random.Next(1, 5);

        if (entities.TryGetComponent(uid, out DndGuidanceComponent? guidance) && timing.CurTime < guidance.Until)
        {
            bonus += random.Next(1, 5);
            entities.RemoveComponent<DndGuidanceComponent>(uid);
        }

        return bonus;
    }
}
