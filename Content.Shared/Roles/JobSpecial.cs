using Robust.Shared.Prototypes;

namespace Content.Shared.Roles
{
    /// <summary>
    /// Provides special hooks for when jobs get spawned in/equipped.
    /// TODO: This is being/should be utilized by more than jobs, and is really just a way to assign components/implants/status effects upon spawning. Rename this class and its derivatives in the future!
    /// TODO: Move derivatives from Server to Shared, probably.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class JobSpecial
    {
        public abstract void AfterEquip(EntityUid mob);

        // CrystallEdge: lets a special know which job granted it (e.g. to badge skills with their
        // source profession), without forcing every existing special to take on a job parameter.
        public virtual void AfterEquip(EntityUid mob, ProtoId<JobPrototype>? job) => AfterEquip(mob);
        // CrystallEdge end
    }
}
