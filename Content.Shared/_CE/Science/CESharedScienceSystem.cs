namespace Content.Shared._CE.Science;

public abstract partial class CESharedScienceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeAchievement();
    }
}
