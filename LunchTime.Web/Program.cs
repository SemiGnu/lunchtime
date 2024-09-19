using LunchTime.Client;
using LunchTime.Web;
using LunchTime.Web.Components;
using System.Globalization;
using Microsoft.JSInterop;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// builder.Services.AddRazorComponents()
//     .AddInteractiveServerComponents();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();

builder.Services.AddHttpClient();
builder.Services.AddLocalization();


builder.Services.Configure<MenuClient.Options>(builder.Configuration.GetSection(MenuClient.Options.SectionName));
builder.Services.AddScoped<IMenuClient, MenuClient>();

var app = builder.Build();

var supportedCultures = new[] { "nb-NO", "en-US", "uk-UA" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();