using System.Linq;
using Content.Shared._CE.LockKey;
using Content.Shared._CE.LockKey.Components;
using Content.Shared.GameTicking;
using Content.Shared.Labels.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.LockKey;

public sealed partial class CELockKeySystem : CESharedLockKeySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LabelSystem _label = default!;

    //TODO: it won't survive saving and loading. This data must be stored in some component.
    private Dictionary<ProtoId<CELockTypePrototype>, List<int>> _roundKeyData = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundEnd);

        SubscribeLocalEvent<CELockComponent, MapInitEvent>(OnLockInit);
        SubscribeLocalEvent<CELockRandomShapeComponent, MapInitEvent>(OnLockRandomInit);

        SubscribeLocalEvent<CEKeyComponent, MapInitEvent>(OnKeyInit);
    }

    #region Init
    private void OnRoundEnd(RoundRestartCleanupEvent ev)
    {
        _roundKeyData = new();
    }

    private void OnKeyInit(Entity<CEKeyComponent> keyEnt, ref MapInitEvent args)
    {
        if (keyEnt.Comp.AutoGenerateShape is null)
            return;

        TrySetShapeFromProto(keyEnt, keyEnt.Comp.AutoGenerateShape.Value);
    }

    private void OnLockInit(Entity<CELockComponent> lockEnt, ref MapInitEvent args)
    {
        if (lockEnt.Comp.AutoGenerateShape is null)
            return;

        TrySetShapeFromProto(lockEnt, lockEnt.Comp.AutoGenerateShape.Value);
    }

    private void OnLockRandomInit(Entity<CELockRandomShapeComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<CELockComponent>(ent, out var lockComp))
            return;
        var shape = new List<int>();
        for (var i = 0; i < ent.Comp.Length; i++)
        {
            shape.Add(_random.Next(-DepthComplexity, DepthComplexity));
        }

        TrySetShape((ent, lockComp), shape);
    }
    #endregion

    private List<int> GetKeyLockData(ProtoId<CELockTypePrototype> category)
    {
        if (_roundKeyData.TryGetValue(category, out var value))
            return new List<int>(value);

        var newData = GenerateNewUniqueLockData(category);
        _roundKeyData[category] = newData;
        return newData;
    }

    private bool TrySetShapeFromProto(Entity<CEKeyComponent> keyEnt, ProtoId<CELockTypePrototype> type)
    {
        if (!TrySetShape((keyEnt, keyEnt.Comp), GetKeyLockData(type)))
            return false;

        var indexedType = _proto.Index(type);
        if (indexedType.Name is not null)
            _label.Label(keyEnt, Loc.GetString(indexedType.Name.Value));

        return true;
    }

    private bool TrySetShapeFromProto(Entity<CELockComponent> lockEnt, ProtoId<CELockTypePrototype> type)
    {
        if (!TrySetShape((lockEnt, lockEnt.Comp), GetKeyLockData(type)))
            return false;

        var indexedType = _proto.Index(type);
        if (indexedType.Name is not null)
            _label.Label(lockEnt, Loc.GetString(indexedType.Name.Value));

        return true;
    }

    private List<int> GenerateNewUniqueLockData(ProtoId<CELockTypePrototype> type)
    {
        List<int> newKeyData = new();
        var categoryData = _proto.Index(type);
        var iteration = 0;

        while (true)
        {
            //Generate try
            newKeyData = new List<int>();
            for (var i = 0; i < categoryData.Complexity; i++)
            {
                newKeyData.Add(_random.Next(-DepthComplexity, DepthComplexity));
            }

            // Identity Check shit code
            // It is currently trying to generate a unique code. If it fails to generate a unique code 100 times, it will output the last generated non-unique code.
            var unique = true;
            foreach (var pair in _roundKeyData)
            {
                if (newKeyData.SequenceEqual(pair.Value))
                {
                    unique = false;
                    break;
                }
            }
            if (unique)
                return newKeyData;

            iteration++;

            if (iteration > 100)
                break;
        }
        Log.Error($"The unique key {type} for CELockKeySystem could not be generated!");
        return newKeyData; //FUCK
    }
}
