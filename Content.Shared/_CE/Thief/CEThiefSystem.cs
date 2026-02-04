using Content.Shared._CE.Thief;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Thief;

public sealed partial class CEThiefSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly EntProtoId _vfx = "CETreasureSparkVFX";
    private readonly SoundSpecifier _sound = new SoundPathSpecifier("/Audio/_CE/Effects/treasure_effect.ogg");
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, CEThiefShowTreasuresEvent>(OnShowTreasures);
    }

    private void OnShowTreasures(Entity<ActorComponent> ent, ref CEThiefShowTreasuresEvent args)
    {
        args.Handled = true;

        var query = EntityQueryEnumerator<CETheftValueComponent, TransformComponent>();
        var count = 0;

        while (query.MoveNext(out _, out _, out var transform))
        {
            var entCoords = _transform.GetWorldPosition(ent.Owner);
            var trsCoords = _transform.GetWorldPosition(transform);

            if ((entCoords - trsCoords).Length() > args.SenseDistance)
                continue;

            count += 1;

            SpawnAtPosition(_vfx, transform.Coordinates);
            _audio.PlayPvs(_sound, transform.Coordinates);
        }
        _popup.PopupEntity(Loc.GetString("ce-action-thief-show-treasures", ("amount", count)), ent, ent);
    }
}
