using System.Numerics;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;

namespace Content.Server._CE.Containers;

/// <summary>Routes held items to nearby connected item-slot fixtures after local insertion is exhausted.</summary>
public sealed partial class CEConnectedItemSlotsSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteractUsing(Entity<CEConnectedItemSlotsComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!args.Handled && args.CanReach && TryInsertFromHand(args.User, args.Used, ent.Owner, out _))
            args.Handled = true;
    }

    public bool TryInsertFromHand(EntityUid user, EntityUid item, EntityUid origin, out EntityUid destination)
    {
        destination = default;
        foreach (var candidate in GetMembersByDistance(origin))
        {
            // A failed transfer can leave an item outside if normal pickup restoration is blocked.
            if (!_hands.IsHolding(user, item))
                return false;
            if (!_slots.TryInsertEmpty(candidate, item, user))
                continue;

            destination = candidate;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the connected group in deterministic breadth-first order.
    /// </summary>
    private IReadOnlyList<EntityUid> GetMembersByDistance(EntityUid origin)
    {
        var result = new List<EntityUid>();
        if (TerminatingOrDeleted(origin) ||
            !TryComp<CEConnectedItemSlotsComponent>(origin, out var connected) ||
            !HasComp<ItemSlotsComponent>(origin))
            return result;

        result.Add(origin);
        if (!TryComp<NodeContainerComponent>(origin, out var nodes) ||
            !nodes.Nodes.TryGetValue(connected.Node, out var originNode))
            return result;

        var pending = new Queue<Node>();
        var visited = new HashSet<EntityUid> { origin };
        pending.Enqueue(originNode);

        while (pending.TryDequeue(out var current))
        {
            foreach (var neighbour in GetNeighbours(current, connected.Group, connected.Node))
            {
                if (!visited.Add(neighbour.Owner))
                    continue;

                result.Add(neighbour.Owner);
                pending.Enqueue(neighbour);
            }
        }

        return result;
    }

    private IReadOnlyList<Node> GetNeighbours(Node current, string group, string nodeName)
    {
        var candidates = new List<ConnectedNeighbour>();
        if (!TryComp(current.Owner, out TransformComponent? currentTransform) ||
            !currentTransform.Anchored ||
            currentTransform.GridUid is not { } gridUid)
            return Array.Empty<Node>();

        foreach (var candidate in current.ReachableNodes)
        {
            if (candidate.Owner == current.Owner || TerminatingOrDeleted(candidate.Owner) ||
                !HasComp<ItemSlotsComponent>(candidate.Owner) ||
                !TryComp<CEConnectedItemSlotsComponent>(candidate.Owner, out var connected) ||
                !string.Equals(connected.Group, group, StringComparison.Ordinal) ||
                !string.Equals(connected.Node, nodeName, StringComparison.Ordinal) ||
                !string.Equals(candidate.Name, nodeName, StringComparison.Ordinal) ||
                !TryComp<NodeContainerComponent>(candidate.Owner, out var candidateNodes) ||
                !candidateNodes.Nodes.TryGetValue(nodeName, out var configuredNode) ||
                !ReferenceEquals(configuredNode, candidate) ||
                !TryComp(candidate.Owner, out TransformComponent? candidateTransform) ||
                !candidateTransform.Anchored ||
                candidateTransform.GridUid != gridUid ||
                !TryGetCardinalDirection(
                    candidateTransform.LocalPosition - currentTransform.LocalPosition,
                    out var direction))
                continue;

            candidates.Add(new ConnectedNeighbour(candidate, direction));
        }

        candidates.Sort(static (left, right) =>
        {
            var direction = left.Direction.CompareTo(right.Direction);
            return direction != 0
                ? direction
                : left.Node.Owner.CompareTo(right.Node.Owner);
        });

        var result = new Node[candidates.Count];
        for (var i = 0; i < candidates.Count; i++)
            result[i] = candidates[i].Node;

        return result;
    }

    private static bool TryGetCardinalDirection(Vector2 delta, out CardinalDirection direction)
    {
        const float tolerance = 0.001f;
        if (MathF.Abs(delta.X) <= tolerance && MathF.Abs(delta.Y - 1f) <= tolerance)
        {
            direction = CardinalDirection.North;
            return true;
        }

        if (MathF.Abs(delta.X) <= tolerance && MathF.Abs(delta.Y + 1f) <= tolerance)
        {
            direction = CardinalDirection.South;
            return true;
        }

        if (MathF.Abs(delta.X - 1f) <= tolerance && MathF.Abs(delta.Y) <= tolerance)
        {
            direction = CardinalDirection.East;
            return true;
        }

        if (MathF.Abs(delta.X + 1f) <= tolerance && MathF.Abs(delta.Y) <= tolerance)
        {
            direction = CardinalDirection.West;
            return true;
        }

        direction = default;
        return false;
    }

    private enum CardinalDirection
    {
        North,
        South,
        East,
        West,
    }

    private readonly record struct ConnectedNeighbour(Node Node, CardinalDirection Direction);
}
