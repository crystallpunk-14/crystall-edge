#nullable enable
using Content.Shared.CCVar;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests._CE;

[TestFixture]
public sealed class CEVerbCategoryLanguageTest
{
    /// <summary>
    /// Verb categories are static and live for the whole process. Their text must follow the server language,
    /// otherwise the client and server compare verbs with different category texts after a language switch.
    /// </summary>
    [Test]
    public async Task VerbCategoryTextFollowsLanguageSwitch()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var cfg = server.ResolveDependency<IConfigurationManager>();

        string en = default!;
        string ru = default!;
        string enAgain = default!;

        await server.WaitPost(() =>
        {
            en = VerbCategory.Rotate.Text;
            cfg.SetCVar(CCVars.ServerLanguage, "ru-RU");
        });

        await server.WaitPost(() =>
        {
            ru = VerbCategory.Rotate.Text;
            cfg.SetCVar(CCVars.ServerLanguage, "en-US");
        });

        await server.WaitPost(() => enAgain = VerbCategory.Rotate.Text);

        Assert.Multiple(() =>
        {
            Assert.That(en, Is.EqualTo("Rotate"));
            Assert.That(ru, Is.EqualTo("Повернуть"), "static category text did not follow the language switch");
            Assert.That(enAgain, Is.EqualTo("Rotate"));
        });

        await pair.CleanReturnAsync();
    }
}
