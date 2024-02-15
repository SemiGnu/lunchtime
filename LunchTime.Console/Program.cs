using Cocona;
using Microsoft.Extensions.DependencyInjection;
using LunchTime.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = CoconaApp.CreateBuilder();
builder.Services.AddHttpClient();
builder.Logging.SetMinimumLevel(LogLevel.Error);
builder.Services.AddScoped<MenuClient>(x => new MenuClient(x.GetRequiredService<IHttpClientFactory>().CreateClient(), Options.Create(new MenuClient.Options
{
    MenuApiUrl = ""
})));
var app = builder.Build();

app.AddCommand(async (
        MenuClient client,
        [Option("tomorrow", ['t'], Description = "Get menu for tomorrow")]
        bool tomorrow,
        [Option("locale", ['l'], Description = "Translate to locale")]
        string? locale
    ) =>
    {
        var menu = await client.GetMenuAsync(tomorrow, locale, CancellationToken.None);
        Console.WriteLine(menu.MainMenu);
        Console.WriteLine(menu.SuppeMenu);
    })
    .WithDescription("Get the menu, dawg.");

app.Run();
