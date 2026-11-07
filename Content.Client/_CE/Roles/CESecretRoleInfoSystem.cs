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

    /// <summary>
    /// Raised with the entity, its current secret role and its original one.
    /// </summary>
    public event Action<EntityUid, ProtoId<CESecretRolePrototype>?, ProtoId<CESecretRolePrototype>?>? OnSecretRoleUpdate;

    /// <summary>
    /// Raised when the server asks to show the local player their objectives after a secret role grant.
    /// </summary>
    public event Action? OnOpenObjectivesMenu;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CESecretRoleInfoEvent>(OnSecretRoleInfoEvent);
    }

    [SubscribeNetworkEvent]
    private void OnOpenCharacterMenuEvent(CEOpenCharacterMenuEvent msg)
    {
        OnOpenObjectivesMenu?.Invoke();
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
        OnSecretRoleUpdate?.Invoke(GetEntity(msg.NetEntity), msg.SecretRole, msg.OriginalSecretRole);
    }
}
