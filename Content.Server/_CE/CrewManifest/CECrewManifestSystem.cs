using Content.Shared.Forensics.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;

namespace Content.Server._CE.CrewManifest;

/// <summary>
///     CE characters don't carry ID cards, so the vanilla station record creation
///     (which requires one) never runs and the crew manifest stays empty.
///     This creates the record straight from the job the player spawned as instead.
/// </summary>
public sealed partial class CECrewManifestSystem : EntitySystem
{
    [Dependency] private StationRecordsSystem _stationRecords = default!;
    [Dependency] private InventorySystem _inventory = default!;

    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (string.IsNullOrEmpty(ev.JobId) || !ProtoMan.HasIndex<JobPrototype>(ev.JobId))
            return;

        if (!TryComp<StationRecordsComponent>(ev.Station, out var records))
            return;

        _inventory.TryGetSlotEntity(ev.Mob, "id", out var idUid);
        TryComp<FingerprintComponent>(ev.Mob, out var fingerprint);
        TryComp<DnaComponent>(ev.Mob, out var dna);

        _stationRecords.CreateGeneralRecord(
            (ev.Station, records),
            idUid,
            ev.Profile.Name,
            ev.Profile.Age,
            ev.Profile.Species,
            ev.Profile.Gender,
            ev.JobId,
            fingerprint?.Fingerprint,
            dna?.DNA,
            ev.Profile);
    }
}
