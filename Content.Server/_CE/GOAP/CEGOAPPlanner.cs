using System.Numerics;
using Content.Shared._CE.GOAP;

namespace Content.Server._CE.GOAP;

/// <summary>
/// GOAP planner using forward A* search with bitmask-packed state.
/// String keys are mapped to bit indices once per call, so the search
/// operates entirely on integers — minimal heap allocations during A*.
/// </summary>
public sealed class CEGOAPPlanner
{
    private struct PlanNode
    {
        public int State;
        public int ActionIndex;  // -1 for start node
        public int ParentIndex;  // -1 for start node
        public float GCost;
        public float HCost;
    }

    private struct CompiledAction
    {
        public int PrecMask;
        public int PrecRequired;
        public int EffMask;
        public int EffRequired;
        public float Cost;
    }

    // Reusable structures cleared at the start of each Plan() call.
    // Held as instance fields so each CEGOAPSystem gets its own planner
    // with no shared mutable state between systems or test runs.
    private readonly Dictionary<string, int> _keyMap = new();
    private readonly List<CompiledAction> _compiledActions = new();
    private readonly List<PlanNode> _nodes = new();
    private readonly MinHeap _openList = new();

    private sealed class MinHeap
    {
        private readonly List<(float Priority, int Element)> _data = new();

        public int Count => _data.Count;

        public void Clear() => _data.Clear();

        public void Enqueue(int element, float priority)
        {
            _data.Add((priority, element));
            SiftUp(_data.Count - 1);
        }

        public int Dequeue()
        {
            var result = _data[0].Element;
            var last = _data.Count - 1;
            _data[0] = _data[last];
            _data.RemoveAt(last);
            if (_data.Count > 0)
                SiftDown(0);
            return result;
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                var parent = (i - 1) / 2;
                if (_data[parent].Priority <= _data[i].Priority)
                    break;
                (_data[parent], _data[i]) = (_data[i], _data[parent]);
                i = parent;
            }
        }

        private void SiftDown(int i)
        {
            var count = _data.Count;
            while (true)
            {
                var smallest = i;
                var left = 2 * i + 1;
                var right = 2 * i + 2;
                if (left < count && _data[left].Priority < _data[smallest].Priority)
                    smallest = left;
                if (right < count && _data[right].Priority < _data[smallest].Priority)
                    smallest = right;
                if (smallest == i)
                    break;
                (_data[smallest], _data[i]) = (_data[i], _data[smallest]);
                i = smallest;
            }
        }
    }
    private readonly HashSet<int> _closedStates = new();

    /// <summary>
    /// Plans a sequence of actions to achieve the goal from the current state.
    /// Returns true if a plan was found and populates the output plan list.
    /// </summary>
    public bool Plan(
        Dictionary<string, bool> currentState,
        Dictionary<string, bool> goalState,
        List<CEGOAPAction> availableActions,
        List<CEGOAPAction> outPlan,
        int maxIterations = 100)
    {
        _keyMap.Clear();
        _compiledActions.Clear();
        _nodes.Clear();
        _openList.Clear();
        _closedStates.Clear();

        BuildKeyMap(currentState, goalState, availableActions);

        var startBits = ToBitmask(currentState);
        ToBitmaskCondition(goalState, out var goalMask, out var goalRequired);

        foreach (var action in availableActions)
        {
            ToBitmaskCondition(action.Preconditions, out var precMask, out var precReq);
            ToBitmaskCondition(action.Effects, out var effMask, out var effReq);
            _compiledActions.Add(new CompiledAction
            {
                PrecMask = precMask,
                PrecRequired = precReq,
                EffMask = effMask,
                EffRequired = effReq,
                Cost = action.Cost,
            });
        }

        var hStart = Heuristic(startBits, goalMask, goalRequired);
        var startIdx = AddNode(startBits, -1, -1, 0f, hStart);
        _openList.Enqueue(startIdx, hStart);

        var iterations = 0;
        while (_openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            var currentIdx = _openList.Dequeue();
            var current = _nodes[currentIdx];

            if ((current.State & goalMask) == goalRequired)
            {
                ReconstructPlan(currentIdx, availableActions, outPlan);
                return true;
            }

            if (!_closedStates.Add(current.State))
                continue;

            for (var i = 0; i < _compiledActions.Count; i++)
            {
                var compiled = _compiledActions[i];

                if ((current.State & compiled.PrecMask) != compiled.PrecRequired)
                    continue;

                var newState = (current.State & ~compiled.EffMask) | compiled.EffRequired;

                if (_closedStates.Contains(newState))
                    continue;

                var gCost = current.GCost + compiled.Cost;
                var hCost = Heuristic(newState, goalMask, goalRequired);
                var newIdx = AddNode(newState, i, currentIdx, gCost, hCost);
                _openList.Enqueue(newIdx, gCost + hCost);
            }
        }

        return false;
    }

    private int AddNode(int state, int actionIndex, int parentIndex, float gCost, float hCost)
    {
        var idx = _nodes.Count;
        _nodes.Add(new PlanNode
        {
            State = state,
            ActionIndex = actionIndex,
            ParentIndex = parentIndex,
            GCost = gCost,
            HCost = hCost,
        });
        return idx;
    }

    private void BuildKeyMap(
        Dictionary<string, bool> currentState,
        Dictionary<string, bool> goalState,
        List<CEGOAPAction> actions)
    {
        foreach (var key in currentState.Keys)
        {
            TryAddKey(key);
        }

        foreach (var key in goalState.Keys)
        {
            TryAddKey(key);
        }

        foreach (var action in actions)
        {
            foreach (var key in action.Preconditions.Keys)
            {
                TryAddKey(key);
            }

            foreach (var key in action.Effects.Keys)
            {
                TryAddKey(key);
            }
        }
    }

    private void TryAddKey(string key)
    {
        if (!_keyMap.ContainsKey(key))
            _keyMap[key] = _keyMap.Count;
    }

    private int ToBitmask(Dictionary<string, bool> state)
    {
        var bits = 0;
        foreach (var (key, value) in state)
        {
            if (value && _keyMap.TryGetValue(key, out var index))
                bits |= 1 << index;
        }

        return bits;
    }

    private void ToBitmaskCondition(
        Dictionary<string, bool> conditions,
        out int mask,
        out int required)
    {
        mask = 0;
        required = 0;
        foreach (var (key, value) in conditions)
        {
            if (!_keyMap.TryGetValue(key, out var index))
                continue;

            mask |= 1 << index;
            if (value)
                required |= 1 << index;
        }
    }

    private static float Heuristic(int state, int goalMask, int goalRequired)
    {
        var diff = (state ^ goalRequired) & goalMask;
        return BitOperations.PopCount((uint) diff);
    }

    private void ReconstructPlan(
        int goalNodeIndex,
        List<CEGOAPAction> availableActions,
        List<CEGOAPAction> outPlan)
    {
        outPlan.Clear();

        var idx = goalNodeIndex;
        while (idx >= 0)
        {
            var node = _nodes[idx];
            if (node.ActionIndex >= 0)
                outPlan.Add(availableActions[node.ActionIndex]);
            idx = node.ParentIndex;
        }

        outPlan.Reverse();
    }
}
