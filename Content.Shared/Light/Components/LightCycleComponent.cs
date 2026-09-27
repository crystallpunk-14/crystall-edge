using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;

namespace Content.Shared.Light.Components;

/// <summary>
/// Cycles through colors AKA "Day / Night cycle" on <see cref="MapLightComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LightCycleComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color OriginalColor = Color.Transparent;

    /// <summary>
    /// How long an entire cycle lasts
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromMinutes(60.0 / 7); //CrystallEdge 30 -> 60/7 minutes, 7 days per 60 min murk collapse timer

    [DataField, AutoNetworkedField]
    public TimeSpan Offset = TimeSpan.FromMinutes(25.0 / 7); //CrystallEdge 5/12 of the day, same start time of day as before

    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Should the offset be randomised upon MapInit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool InitialOffset = false; //CrystallEdge false default, for znetwork syncing

    /// <summary>
    /// Trench of the oscillation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinLightLevel = 0.3f; //CrystallEdge increase from 0 to 0.3

    /// <summary>
    /// Peak of the oscillation
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxLightLevel = 3f;

    [DataField, AutoNetworkedField]
    public float ClipLight = 1.25f;

    [DataField, AutoNetworkedField]
    public Color ClipLevel = new Color(1f, 1f, 1.25f);

    [DataField, AutoNetworkedField]
    public Color MinLevel = new Color(0.1f, 0.15f, 0.50f);

    [DataField, AutoNetworkedField]
    public Color MaxLevel = new Color(2f, 2f, 5f);
}
