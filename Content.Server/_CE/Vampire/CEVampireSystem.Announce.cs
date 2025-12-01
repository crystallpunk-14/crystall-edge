using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Systems;
using Content.Shared._CE.Vampire.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server._CE.Vampire;

public sealed partial class CEVampireSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IAdminManager _admin = default!;

    private void InitializeAnnounces()
    {
        SubscribeLocalEvent<CEVampireClanHeartComponent, MapInitEvent>(OnHeartCreate);
        SubscribeLocalEvent<CEVampireClanHeartComponent, DamageChangedEvent>(OnHeartDamaged);
        SubscribeLocalEvent<CEVampireClanHeartComponent, ComponentRemove>(OnHeartDestructed);
    }

    private void OnHeartCreate(Entity<CEVampireClanHeartComponent> ent, ref MapInitEvent args)
    {
        HashSet<Entity<CEVampireComponent>> nearbyVampires = new();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, 10f, nearbyVampires);

        if (nearbyVampires.Count > 0)
        {
            ent.Comp.VampireOwner = nearbyVampires.First();
            Dirty(ent);
        }

        if (ent.Comp.VampireOwner is null)
            return;

        AnnounceToVampire(ent.Comp.VampireOwner.Value, Loc.GetString("ce-vampire-tree-created"));
        AnnounceToEnemyVampires(ent.Comp.VampireOwner.Value, Loc.GetString("ce-vampire-tree-created"));
    }

    private void OnHeartDamaged(Entity<CEVampireClanHeartComponent> ent, ref DamageChangedEvent args)
    {
        if (ent.Comp.VampireOwner is null)
            return;

        if (!args.DamageIncreased)
            return;

        if (_timing.CurTime < ent.Comp.NextAnnounceTime)
            return;

        ent.Comp.NextAnnounceTime = _timing.CurTime + ent.Comp.MaxAnnounceFreq;

        AnnounceToVampire(ent.Comp.VampireOwner.Value, Loc.GetString("ce-vampire-tree-damaged"));
    }

    private void OnHeartDestructed(Entity<CEVampireClanHeartComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.VampireOwner is null)
            return;

        AnnounceToVampire(ent.Comp.VampireOwner.Value, Loc.GetString("ce-vampire-tree-destroyed-self"));
        AnnounceToEnemyVampires(ent.Comp.VampireOwner.Value, Loc.GetString("ce-vampire-tree-destroyed"));
    }

    public void AnnounceToVampire(EntityUid ent, string message)
    {
        if (!HasComp<CEVampireComponent>(ent))
            return;
        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var filter = Filter.Empty();
        filter.AddPlayer(actor.PlayerSession);

        VampireAnnounce(filter, message);
    }

    public void AnnounceToEnemyVampires(EntityUid ent, string message)
    {
        if (!HasComp<CEVampireComponent>(ent))
            return;

        var filter = Filter.Empty();
        var query = EntityQueryEnumerator<CEVampireComponent, ActorComponent>();

        while (query.MoveNext(out var uid, out var vampire, out var actor))
        {
            if (uid == ent)
                continue;

            filter.AddPlayer(actor.PlayerSession);
        }

        filter.AddPlayers(_admin.ActiveAdmins);

        if (filter.Count == 0)
            return;

        VampireAnnounce(filter, message);
    }

    private void VampireAnnounce(Filter players, string message)
    {
        _chat.DispatchFilteredAnnouncement(
            players,
            message,
            sender: Loc.GetString("ce-vampire-sender"),
            announcementSound: new SoundPathSpecifier("/Audio/_CE/Announce/vampire.ogg"),
            colorOverride: Color.FromHex("#820e22"));
    }
}
