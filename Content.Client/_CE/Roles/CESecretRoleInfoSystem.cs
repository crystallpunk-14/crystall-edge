using Content.Shared._CE.Roles;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Roles;

/// <summary>
/// Client side of <see cref="CESecretRoleInfoSystem"/> (server). Kept separate from
/// <c>CharacterInfoSystem</c> - see <see cref="CESecretRoleInfoEvent"/> for why.
/// </summary>
public sealed partial class CESecretRoleInfoSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;

    public event Action<EntityUid, ProtoId<CESecretRolePrototype>?>? OnSecretRoleUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CESecretRoleInfoEvent>(OnSecretRoleInfoEvent);
    }

    public void RequestSecretRoleInfo()
    {
        var entity = _players.LocalEntity;
        if (entity == null)
            return;

        RaiseNetworkEvent(new CERequestSecretRoleInfoEvent(GetNetEntity(entity.Value)));
    }

    private void OnSecretRoleInfoEvent(CESecretRoleInfoEvent msg, EntitySessionEventArgs args)
    {
        OnSecretRoleUpdate?.Invoke(GetEntity(msg.NetEntity), msg.SecretRole);
    }
}
