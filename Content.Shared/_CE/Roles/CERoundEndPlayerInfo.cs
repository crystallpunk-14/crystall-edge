using Content.Shared._CE.Roles;

namespace Content.Shared.GameTicking;

/// <summary>
/// Extends <see cref="RoundEndMessageEvent.RoundEndPlayerInfo"/> (declared <c>partial</c> upstream)
/// with the player's secret role, revealed in the round-end player manifest. Kept out of the
/// upstream file entirely since the struct is already partial for exactly this purpose.
/// </summary>
public partial class RoundEndMessageEvent
{
    public partial struct RoundEndPlayerInfo
    {
        /// <summary>
        /// LocId of the secret role's display name (<see cref="Content.Shared._CE.Roles.CESecretRolePrototype.Name"/>),
        /// or null if the player never held one - localized client-side the same way <see cref="Role"/> is.
        /// </summary>
        public string? SecretRole;

        /// <summary>
        /// The secret role's faction color (<see cref="CESecretDepartmentPrototype.Color"/>), used to
        /// tint <see cref="SecretRole"/> in the manifest. Meaningless when <see cref="SecretRole"/> is null.
        /// </summary>
        public Color SecretRoleColor;
    }
}
