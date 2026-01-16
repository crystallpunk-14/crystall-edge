using System.Text;
using System.Threading.Tasks;
using Content.Server.Discord;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    private const int DiscordMessageMaxLength = 2000;
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

        var cleanText = FormattedMessage.RemoveMarkupPermissive(sb.ToString());
        SendRoundEndSummaryDiscordMessage(cleanText);
    }

    private async void SendRoundEndSummaryDiscordMessage(string roundEndSummary)
    {
        try
        {
            if (_roundEndWebhookIdentifier == null)
                return;

            // Split message into chunks if it exceeds Discord's character limit
            if (roundEndSummary.Length <= DiscordMessageMaxLength)
            {
                var payload = new WebhookPayload { Content = roundEndSummary };
                await _discord.CreateMessage(_roundEndWebhookIdentifier.Value, payload);
            }
            else
            {
                // Split the message into multiple parts
                var chunks = SplitMessage(roundEndSummary, DiscordMessageMaxLength);
                for (var i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];

                    var payload = new WebhookPayload { Content = chunk };
                    await _discord.CreateMessage(_roundEndWebhookIdentifier.Value, payload);

                    // Small delay between messages to avoid rate limiting
                    if (i < chunks.Count - 1)
                        await Task.Delay(500);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error while sending discord round end summary message:\n{e}");
        }
    }

    private List<string> SplitMessage(string message, int maxLength)
    {
        var chunks = new List<string>();

        // Use the provided maxLength as the limit for each chunk.
        var effectiveMaxLength = maxLength - 20;

        if (message.Length <= effectiveMaxLength)
        {
            chunks.Add(message);
            return chunks;
        }

        var lines = message.Split('\n');
        var currentChunk = new StringBuilder();

        foreach (var line in lines)
        {
            // If adding this line would exceed the limit
            if (currentChunk.Length + line.Length + 1 > effectiveMaxLength)
            {
                // Save current chunk if it's not empty
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString());
                    currentChunk.Clear();
                }

                // If a single line is too long, split it by words
                if (line.Length > effectiveMaxLength)
                {
                    var words = line.Split(' ');
                    foreach (var word in words)
                    {
                        if (currentChunk.Length + word.Length + 1 > effectiveMaxLength)
                        {
                            if (currentChunk.Length > 0)
                            {
                                chunks.Add(currentChunk.ToString());
                                currentChunk.Clear();
                            }

                            // If even a single word is too long, truncate it
                            if (word.Length > effectiveMaxLength)
                            {
                                chunks.Add(word[..(effectiveMaxLength - 3)] + "...");
                            }
                            else
                            {
                                currentChunk.Append(word);
                            }
                        }
                        else
                        {
                            if (currentChunk.Length > 0)
                                currentChunk.Append(' ');
                            currentChunk.Append(word);
                        }
                    }
                    currentChunk.AppendLine();
                }
                else
                {
                    currentChunk.AppendLine(line);
                }
            }
            else
            {
                currentChunk.AppendLine(line);
            }
        }

        // Add the last chunk if it's not empty
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString());
        }

        return chunks;
    }
}
