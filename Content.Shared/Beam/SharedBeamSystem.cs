namespace Content.Shared.Beam;

public abstract class SharedBeamSystem : EntitySystem
{
    // WD EDIT START
    public virtual void TryCreateBeam(EntityUid user,
        EntityUid target,
        string bodyPrototype,
        string? bodyState = null,
        string shader = "unshaded",
        EntityUid? controller = null)
    {

    }
    // WD EDIT END
}
