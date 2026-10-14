namespace Vixly.Shared.UI.Gallery;

// =========================================================
// FILE: Program.cs
//
// PURPOSE:
// Entry point for the Shared.UI component gallery.
//
// RESPONSIBILITIES:
// - Host the standalone gallery without any database or backend
// - Configure interactive server rendering for component demos
//
// IMPORTANT NOTES:
// - May reference Vixly.Shared.UI only (Global Standard 3)
// - Sample data only; no real tenant or business data
// =========================================================
public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        WebApplication app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
