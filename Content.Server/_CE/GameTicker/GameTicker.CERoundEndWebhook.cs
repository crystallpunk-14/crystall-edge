using System.Text;
using Content.Server.Discord;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    private WebhookIdentifier? _roundEndWebhookIdentifier;
    private void InitializeCrystallEdgeRoundEndWebhook()
    {
        Subs.CVar(_cfg, CCVars.CEDiscordRoundEndSummaryWebhook, value =>
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _discord.GetWebhook(value, data => _roundEndWebhookIdentifier = data.ToIdentifier());
            }
        }, true);
    }

    private void RoundEndSummarySendToDiscord(RoundEndMessageEvent ev)
    {
        var sb = new StringBuilder();

        //Round title
        sb.AppendLine("# " + Loc.GetString("round-end-summary-window-round-id-label", ("roundId", ev.RoundId)));
        sb.AppendLine("## " + Loc.GetString("round-end-summary-window-gamemode-name-label", ("gamemode", ev.GamemodeTitle)));

        //Duration
        sb.AppendLine(Loc.GetString("round-end-summary-window-duration-label",
            ("hours", ev.RoundDuration.Hours),
            ("minutes", ev.RoundDuration.Minutes),
            ("seconds", ev.RoundDuration.Seconds)));

        //Round end text
        sb.AppendLine(ev.RoundEndText);

        SendRoundEndSummaryDiscordMessage(sb.ToString());
    }

    private async void SendRoundEndSummaryDiscordMessage(string roundEndSummary)
    {
        try
        {
            if (_roundEndWebhookIdentifier == null)
                return;

            var payload = new WebhookPayload { Content = roundEndSummary };

            await _discord.CreateMessage(_roundEndWebhookIdentifier.Value, payload);
        }
        catch (Exception e)
        {
            Log.Error($"Error while sending discord round end summary message:\n{e}");
        }
    }
}
