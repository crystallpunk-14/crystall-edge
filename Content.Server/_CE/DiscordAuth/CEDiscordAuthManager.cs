using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._CE.DiscordAuth;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._CE.DiscordAuth;

public sealed partial class CEDiscordAuthManager
{
    [Dependency] private IServerNetManager _netMgr = default!;
    [Dependency] private IPlayerManager _playerMgr = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IServerDbManager _db = default!;

    private ISawmill _sawmill = default!;
    private readonly HttpClient _httpClient = new();
    private bool _enabled;
    private string _apiUrl = string.Empty;
    private string _apiKey = string.Empty;

    // CrystallEdge's Discord server. Shared with the bot/webhook integration (CCVars.Discord.GuildId).
    public const string DISCORD_GUILD = "1221923073759121468";

    // Guilds whose members are softbanned from joining, regardless of CrystallEdge-server membership.
    private readonly HashSet<string> _blockedGuilds = new()
    {
        "1346922008000204891",
        "1186566619858731038",
        "1355279097906855968",
        "1352009516941705216",
        "1359476387190145034",
        "1294276016117911594",
        "1278755078315970620",
        "1330772249644630157",
        "1274951101464051846",
    };

    public event EventHandler<ICommonSession>? PlayerVerified;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("discordAuth");

        _cfg.OnValueChanged(CCVars.CEDiscordAuthEnabled, v => _enabled = v, true);
        _cfg.OnValueChanged(CCVars.CETypeAuthUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCVars.CETypeAuthToken, v => _apiKey = v, true);

        _netMgr.RegisterNetMessage<MsgCEDiscordAuthRequired>();
        _netMgr.RegisterNetMessage<MsgCEDiscordAuthCheck>(OnAuthCheck);

        _playerMgr.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private async void OnAuthCheck(MsgCEDiscordAuthCheck msg)
    {
        var verified = await IsVerified(msg.MsgChannel.UserId);
        if (!verified.Verified)
            return;

        var session = _playerMgr.GetSessionById(msg.MsgChannel.UserId);
        PlayerVerified?.Invoke(this, session);
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Connected)
            return;

        if (!_enabled)
        {
            PlayerVerified?.Invoke(this, args.Session);
            return;
        }

        var verified = await IsVerified(args.Session.UserId);
        if (verified.Verified)
        {
            PlayerVerified?.Invoke(this, args.Session);
            return;
        }

        var message = new MsgCEDiscordAuthRequired
        {
            AuthUrl = await GenerateLink(args.Session.UserId) ?? string.Empty,
            ErrorMessage = verified.ErrorMessage,
        };
        args.Session.Channel.SendMessage(message);
    }

    public async Task<AuthData> IsVerified(NetUserId userId, CancellationToken cancel = default)
    {
        _sawmill.Debug($"Player {userId} check Discord verification");

        var requestUrl = $"{_apiUrl}/api/uuid?method=uid&id={userId}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request, cancel);
        var verified = response.StatusCode == HttpStatusCode.OK;

        if (!verified)
            return new AuthData { Verified = false, ErrorMessage = Loc.GetString("ce-discord-info") };

        return await CheckGuilds(userId, cancel);
    }

    private async Task<AuthData> CheckGuilds(NetUserId userId, CancellationToken cancel = default)
    {
        var isWhitelisted = await _db.GetWhitelistStatusAsync(userId);
        if (isWhitelisted)
            return new AuthData { Verified = true };

        _sawmill.Debug($"Checking guilds for {userId}");

        var requestUrl = $"{_apiUrl}/api/guilds?method=uid&id={userId}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request, cancel);
        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Error($"Player {userId} guilds check failed: {(int)response.StatusCode}");
            return new AuthData { Verified = false, ErrorMessage = "Unexpected error while checking Discord guilds." };
        }

        var guilds = await response.Content.ReadFromJsonAsync<DiscordGuildsResponse>(cancel);
        if (guilds is null)
        {
            _sawmill.Error($"Player {userId} guilds check failed: response body was empty");
            return new AuthData { Verified = false, ErrorMessage = "Unexpected error while checking Discord guilds." };
        }

        foreach (var guild in guilds.Guilds)
        {
            if (_blockedGuilds.Contains(guild.Id))
            {
                // Deliberately vague to avoid revealing the softban to the player.
                var errorMessage = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("RXJyb3IgMjcwMQ=="));
                return new AuthData { Verified = false, ErrorMessage = errorMessage };
            }
        }

        if (guilds.Guilds.All(guild => guild.Id != DISCORD_GUILD))
        {
            _sawmill.Debug($"Player {userId} is not in required guild {DISCORD_GUILD}");
            return new AuthData { Verified = false, ErrorMessage = "You are not a member of the CrystallEdge Discord server." };
        }

        return new AuthData { Verified = true };
    }

    public async Task<string?> GenerateLink(NetUserId userId, CancellationToken cancel = default)
    {
        var requestUrl = $"{_apiUrl}/api/link?uid={userId}";

        try
        {
            var response = await _httpClient.GetAsync(requestUrl, cancel);
            if (!response.IsSuccessStatusCode)
                return null;

            var link = await response.Content.ReadFromJsonAsync<DiscordLinkResponse>(cancel);
            return link!.Link;
        }
        catch (HttpRequestException)
        {
            _sawmill.Error("TypeAuth service is unreachable. Check if it's online.");
            return null;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Unexpected error generating Discord auth link. Error: {e.Message}. Stack: \n{e.StackTrace}");
            return null;
        }
    }

    private sealed class DiscordLinkResponse
    {
        [JsonPropertyName("link")]
        public string Link { get; set; } = string.Empty;
    }

    private sealed class DiscordGuildsResponse
    {
        [JsonPropertyName("guilds")]
        public DiscordGuild[] Guilds { get; set; } = [];
    }

    private sealed class DiscordGuild
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;
    }

    public sealed class AuthData
    {
        public bool Verified { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
