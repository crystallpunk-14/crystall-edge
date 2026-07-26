namespace Content.Client._CE.TimedDespawnFadeout;

[RegisterComponent]
[Access(typeof(CETimedDespawnFadeoutSystem))]
public sealed partial class CETimedDespawnFadeoutComponent : Component
{
    public float OriginalLifetime;
}
