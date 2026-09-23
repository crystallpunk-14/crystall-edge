using Robust.Shared.Serialization;

namespace Content.Shared._CE.Character;

/// <summary>
/// A ghost's request to view another player's objectives (for the "info" button next to the
/// warp button in the ghost target window). Objective data hangs off the target's mind, which -
/// unlike Job/health/etc - is never PVS-networked to anyone but its owner, so it can't be read
/// straight off the target entity the way skills can; this needs an explicit server round trip.
/// </summary>
[Serializable, NetSerializable]
public sealed class CEViewCharacterRequestEvent : EntityEventArgs
{
    public NetEntity Target { get; }

    public CEViewCharacterRequestEvent(NetEntity target)
    {
        Target = target;
    }
}

/// <summary>
/// Pre-resolved display data for a single CE objective, since the client can't read the
/// objective entity's own components (also mind-owner-only) directly.
/// </summary>
[Serializable, NetSerializable]
public struct CEObjectiveInfo
{
    public string Title;
    public string Description;
    public float Progress;

    /// <summary>
    /// The objective entity's prototype, resolved to an icon client-side via SpriteSystem.Frame0.
    /// </summary>
    public string? PrototypeId;

    public string? DescriptorName;
    public Color? DescriptorColor;
    public string? DescriptorTooltip;
}

[Serializable, NetSerializable]
public sealed class CEViewCharacterResponseEvent : EntityEventArgs
{
    public NetEntity Target { get; }
    public List<CEObjectiveInfo> Objectives { get; }

    public CEViewCharacterResponseEvent(NetEntity target, List<CEObjectiveInfo> objectives)
    {
        Target = target;
        Objectives = objectives;
    }
}
