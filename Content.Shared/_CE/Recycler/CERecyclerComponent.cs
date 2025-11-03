using System.Numerics;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Recycler;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CESharedRecyclerSystem))]
public sealed partial class CERecyclerComponent : Component
{
    [DataField]
    public string FixtureId = "brrt";

    /// <summary>
    /// a whitelist for what entities can be inserted into this reclaimer
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// a blacklist for what entities cannot be inserted into this reclaimer
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public Vector2 SpawnOffset = new Vector2(0f, 0.5f);
}
