using System.Diagnostics.CodeAnalysis;
using Content.Shared._CE.Sponsor;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Sponsor;

public sealed partial class CEClientSponsorSystem : ICESponsorManager
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IClientNetManager _net = default!;

    private CESponsorRolePrototype? _sponsorRole;

    public void Initialize()
    {
        _net.RegisterNetMessage<CESponsorRoleUpdate>(OnSponsorRoleUpdate);
        _net.Disconnect += NetOnDisconnected;
    }

    private void NetOnDisconnected(object? sender, NetDisconnectedArgs e)
    {
        _sponsorRole = null;
    }

    private void OnSponsorRoleUpdate(CESponsorRoleUpdate msg)
    {
        if (!_proto.TryIndex(msg.Role, out var indexedRole))
            return;

        _sponsorRole = indexedRole;
    }

    public bool TryGetSponsorOOCColor(NetUserId userId, [NotNullWhen(true)] out Color? color)
    {
        color = _sponsorRole?.Color;
        return color is not null;
    }

    public bool UserHasFeature(NetUserId userId, ProtoId<CESponsorFeaturePrototype> feature, bool ifDisabledSponsorship = true)
    {
        if (_sponsorRole is null)
            return false;

        if (!_proto.TryIndex(feature, out var indexedFeature))
            return false;

        return _sponsorRole.Priority >= indexedFeature.MinPriority;
    }
}
