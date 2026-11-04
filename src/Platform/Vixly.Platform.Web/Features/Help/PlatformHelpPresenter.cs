using Vixly.Platform.Application.UseCases.MyAccount.GetMyPlatformAccess;
using Vixly.Shared.Help;
using Vixly.Shared.UI.Presenters;

namespace Vixly.Platform.Web.Features.Help;

// =========================================================
// FILE: PlatformHelpPresenter.cs
//
// PURPOSE:
// Loads Control help using the existing effective-access use case.
//
// RESPONSIBILITIES:
// - Filter home, search, direct, related and contextual lookups.
// - Fall back to unrestricted articles when access cannot load.
//
// IMPORTANT NOTES:
// - Never infer SuperUser from a stale principal or request input.
// - Clear previous results before attempting a fresh access read.
// =========================================================
public sealed class PlatformHelpPresenter : PresenterBase
{
    private readonly PlatformHelpCatalogue _help;
    private readonly GetMyPlatformAccessUseCase _access;
    private readonly PlatformUseCaseScope _scope;
    private int _loadVersion;

    public PlatformHelpPresenter(PlatformHelpCatalogue help, GetMyPlatformAccessUseCase access,
        PlatformUseCaseScope scope, ILogger<PlatformHelpPresenter> logger) : base(logger)
    {
        _help = help ?? throw new ArgumentNullException(nameof(help));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public PlatformHelpState State { get; private set; } = PlatformHelpState.Empty;

    public async Task LoadAsync(string language, string? slug, string? query, string path = "/help")
    {
        int version = Interlocked.Increment(ref _loadVersion);
        State = PlatformHelpState.Empty;
        HelpReader reader = new([], []);
        await RunAsync(async cancellationToken =>
        {
            GetMyPlatformAccessResponse response = await _access.ExecuteAsync(_scope.Begin(), new(), cancellationToken);
            if (response.Succeeded)
                reader = new(response.PermissionCodes, [], superUser: response.IsSuperUser);
        });

        if (version != Volatile.Read(ref _loadVersion)) return;

        string term = (query ?? string.Empty).Trim();
        if (term.Length > 160) term = term[..160];
        HelpArticle? article = slug is null ? null : _help.Catalogue.Find(language, slug, reader);
        IReadOnlyList<HelpArticle> articles = slug is not null ? [] : term.Length == 0
            ? _help.Catalogue.Visible(language, reader) : HelpSearch.Find(_help.Catalogue, language, term, reader);
        State = new(article, articles, article is null ? [] : _help.Catalogue.Related(article, reader),
            term, slug is not null && article is null, _help.ContextAddress(language, path, reader));
        NotifyStateChanged();
    }
}
