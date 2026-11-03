using Robust.Shared.Audio;

namespace Content.Shared._CE.Funnel;

/// <summary>
/// Moves items between the floor and a storage container one tile in front of the funnel.
/// The mode is picked by the conveyor anchored on the funnel tile, see <see cref="CEFunnelMode"/>.
/// Items placed into the funnel's own storage act as a filter; an empty storage lets everything through.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEFunnelComponent : Component
{
    /// <summary>
    /// Hard fixture on the front edge: inserts items in <see cref="CEFunnelMode.Load"/> mode and blocks the ones that can't go in.
    /// </summary>
    [DataField]
    public string FixtureId = "collect";

    /// <summary>
    /// Tile-wide sensor that grabs passing items in <see cref="CEFunnelMode.Side"/> mode.
    /// </summary>
    [DataField]
    public string SideFixtureId = "side";

    [DataField]
    public SoundSpecifier? InsertSound;

    [DataField]
    public SoundSpecifier? EjectSound;

    /// <summary>
    /// How often the funnel extracts an item or retries inserting the blocked ones.
    /// </summary>
    [DataField]
    public TimeSpan ExtractionFrequency = TimeSpan.FromSeconds(1);

    [DataField, AutoPausedField]
    public TimeSpan NextExtractionTime = TimeSpan.Zero;

    /// <summary>
    /// Id of the container on the front tile the funnel works with.
    /// </summary>
    [DataField]
    public string ContainerCheckId = "storagebase";

    /// <summary>
    /// Id of the funnel's own container with filter samples.
    /// </summary>
    [DataField]
    public string FilterContainerId = "storagebase";
}

public enum CEFunnelMode : byte
{
    /// <summary>
    /// No conveyor, or the conveyor moves towards the container: items touching the front edge are inserted.
    /// </summary>
    Load,

    /// <summary>
    /// The conveyor moves away from the container: items are extracted one by one while it is powered.
    /// </summary>
    Unload,

    /// <summary>
    /// The conveyor runs across the funnel: matching items are grabbed from the belt, the rest pass by.
    /// </summary>
    Side,
}
