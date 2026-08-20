using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server._CE.DiscordAuth;
using Content.Shared._CE.Sponsor;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.Sponsor;

public sealed partial class CESponsorSystem : ICESponsorManager
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private CEDiscordAuthManager _discordAuthManager = default!;
    [Dependency] private INetManager _netMgr = default!;
    [Dependency] private IServerNetManager _netManager = default!;

    private readonly HttpClient _httpClient = new();
    private string _apiUrl = string.Empty;
    private string _apiKey = string.Empty;
    private bool _enabled;

    private ISawmill _sawmill = null!;

    private readonly Dictionary<NetUserId, CESponsorRolePrototype> _cachedSponsors = new();

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("sponsors");

        _netManager.RegisterNetMessage<CESponsorRoleUpdate>();

        _cfg.OnValueChanged(CCVars.CESponsorEnabled, val => { _enabled = val; }, true);
        _cfg.OnValueChanged(CCVars.CETypeAuthUrl, val => { _apiUrl = val; }, true);
        _cfg.OnValueChanged(CCVars.CETypeAuthToken, val => { _apiKey = val; }, true);

        _discordAuthManager.PlayerVerified += async (_, e) =>
        {
            await OnPlayerVerified(e);
        };

        _netMgr.Disconnect += OnDisconnect;
    }

    private async Task<List<string>?> GetRoles(NetUserId userId)
    {
        var requestUrl = $"{_apiUrl}/api/roles?method=uid&id={userId}&guildId={CEDiscordAuthManager.DISCORD_GUILD}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Error($"Failed to retrieve roles for user {userId}: {response.StatusCode}");
            return null;
        }

        var responseContent = await response.Content.ReadFromJsonAsync<RolesResponse>();

        if (responseContent is not null)
            return responseContent.Roles.ToList();

        _sawmill.Error($"Roles not found in response for user {userId}");
        return null;
    }

    private async Task OnPlayerVerified(ICommonSession e)
    {
        if (!_enabled)
            return;

        var roles = await GetRoles(e.UserId);
        if (roles is null)
            return;

        CESponsorRolePrototype? targetRole = null;
        foreach (var role in _proto.EnumeratePrototypes<CESponsorRolePrototype>())
        {
            if (!roles.Contains(role.DiscordRoleId))
                continue;

            if (targetRole is null || role.Priority > targetRole.Priority)
                targetRole = role;
        }

        if (targetRole is not null)
            _cachedSponsors[e.UserId] = targetRole;

        if (_cachedSponsors.TryGetValue(e.UserId, out var cachedRole))
        {
            e.Channel.SendMessage(new CESponsorRoleUpdate
            {
                Role = cachedRole,
            });
        }
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        _cachedSponsors.Remove(e.Channel.UserId);
    }

    public bool UserHasFeature(NetUserId userId,
        ProtoId<CESponsorFeaturePrototype> feature,
        bool ifDisabledSponsorship = true)
    {
        if (!_enabled)
            return ifDisabledSponsorship;

        if (!_proto.TryIndex(feature, out var indexedFeature))
            return false;

        if (!_cachedSponsors.TryGetValue(userId, out _))
            return false;

        return _cachedSponsors[userId].Priority >= indexedFeature.MinPriority;
    }

    public bool TryGetSponsorOOCColor(NetUserId userId, [NotNullWhen(true)] out Color? color)
    {
        color = null;

        if (!_enabled)
            return false;

        if (!_cachedSponsors.TryGetValue(userId, out var sponsorRole))
            return false;

        color = sponsorRole.Color;

        return color is not null;
    }

    private sealed class RolesResponse
    {
        [JsonPropertyName("roles")]
        public string[] Roles { get; set; } = [];
    }
}
