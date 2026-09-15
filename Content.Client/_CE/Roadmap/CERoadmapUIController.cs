using Content.Client.Credits;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;

namespace Content.Client._CE.Roadmap;

public sealed partial class CERoadmapUIController : UIController
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IUriOpener _uriOpener = default!;

    private CERoadmapWindow? _window;

    public void ToggleRoadmap()
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
            return;
        }

        _window = new CERoadmapWindow();
        _window.OnClose += () => _window = null;

        if (_config.GetCVar(CCVars.InfoLinksDiscord) is { Length: > 0 } discordLink)
        {
            _window.DiscordButton.Visible = true;
            _window.DiscordButton.OnPressed += _ => _uriOpener.OpenUri(discordLink);
        }

        if (_config.GetCVar(CCVars.InfoLinksPatreon) is { Length: > 0 } patreonLink)
        {
            _window.PatreonButton.Visible = true;
            _window.PatreonButton.OnPressed += _ => _uriOpener.OpenUri(patreonLink);
        }

        _window.CreditsButton.OnPressed += _ => new CreditsWindow().OpenCentered();

        _window.OpenCentered();
        _window.RepopulateRoadmapItems();
    }
}
