using System.Text;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Robust.Server.Containers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.Passport;

public sealed partial class CEPassportSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;


    private int _current_year;
    public readonly EntProtoId PassportProto = "CEPassport";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawning);
        _cfg.OnValueChanged(CCVars.CECurrentYear,
            value => { _current_year = value; },
            true);
    }
    private void OnPlayerSpawning(PlayerSpawnCompleteEvent ev)
    {
        if (!TryComp<InventoryComponent>(ev.Mob, out var inventory))
            return;

        var passport = Spawn(PassportProto, Transform(ev.Mob).Coordinates);

        if (!TryComp<PaperComponent>(passport, out var paper))
            return;

        var text = GeneratePassportText(ev);
        _paper.SetContent((passport, paper), text);
        _paper.TryStamp((passport, paper),
            new StampDisplayInfo
            {
                StampedColor = Color.FromHex("#0a332a"),
                StampedName = Loc.GetString("ce-passport-stamp")
            },
            "");

        //Try to equip passport on pocket 1
        if (_inventory.TryEquip(ev.Mob, passport, "pocket1", inventory: inventory)) return;

        //Try to equip passport on pocket 2
        if (_inventory.TryEquip(ev.Mob, passport, "pocket2", inventory: inventory)) return;

        //Try place passport in backpack
        if (_inventory.TryGetSlotEntity(ev.Mob, "back", out var backEntity))
            if (backEntity.HasValue && TryComp<StorageComponent>(backEntity.Value, out var storageComp))
                if (_container.Insert(passport, storageComp.Container)) return;
    }

    private string GeneratePassportText(PlayerSpawnCompleteEvent ev)
    {
        var sb = new StringBuilder();

        //Name
        sb.AppendLine(Loc.GetString("ce-passport-name", ("name", ev.Profile.Name)));
        //Species
        if (_proto.TryIndex(ev.Profile.Species, out var indexedSpecies))
            sb.AppendLine(Loc.GetString("ce-passport-species", ("species", Loc.GetString(indexedSpecies.Name))));
        //Birthday
        var birthyear = _current_year - ev.Profile.Age;
        var isNegativeBirthyear = birthyear <= 0;
        var birthday = $"{_random.Next(40) + 1}.{_random.Next(12) + 1}.{(isNegativeBirthyear ? $"{Math.Abs(birthyear) + 1} BC" : birthyear)}";
        sb.AppendLine(Loc.GetString("ce-passport-birth-date", ("birthday", birthday)));
        //Job
        if (ev.JobId is not null && _proto.TryIndex<JobPrototype>(ev.JobId, out var indexedJob))
            sb.AppendLine(Loc.GetString("ce-passport-job", ("job", Loc.GetString(indexedJob.Name))));

        return sb.ToString();
    }
}
