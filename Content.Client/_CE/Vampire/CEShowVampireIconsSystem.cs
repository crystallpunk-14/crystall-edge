using Content.Client.Administration.Managers;
using Content.Client.Overlays;
using Content.Shared._CE.Vampire.Components;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Vampire;

public sealed class CEShowVampireIconsSystem : EquipmentHudSystem<CEShowVampireIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IClientAdminManager _admin = default!;

    private readonly ProtoId<FactionIconPrototype> _vampireIcon = "CEVampire";
    private readonly ProtoId<FactionIconPrototype> _vampireHigherIcon = "CEVampireHigher";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEVampireComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(Entity<CEVampireComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        var higher = ent.Comp.HigherVampire;

        if (!_proto.TryIndex(higher ? _vampireHigherIcon : _vampireIcon, out var indexedIcon))
            return;

        // Show icons for admins
        if (_admin.IsAdmin() && HasComp<GhostComponent>(_player.LocalEntity))
        {
            args.StatusIcons.Add(indexedIcon);
            return;
        }

        if (HasComp<CEVampireComponent>(_player.LocalEntity))
        {
            args.StatusIcons.Add(indexedIcon);
            return;
        }
    }
}
