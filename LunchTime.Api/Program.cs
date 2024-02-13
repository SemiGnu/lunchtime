using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenAI_API;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.Configure<LunchTimeOptions>(builder.Configuration.GetSection("LunchTime"));
builder.Services.AddSingleton<LunchTime>();

var app = builder.Build();


app.MapGet("/", (
    [FromServices] LunchTime lunchTime,
    [FromQuery] string? locale = null
) => lunchTime.GetMenu(false, locale));

app.MapGet("/tomorrow", (
    [FromServices] LunchTime lunchTime,
    [FromQuery] string? locale = null
) => lunchTime.GetMenu(true, locale));

app.Run();

public class LunchTimeOptions
{
    public string MenuUrl { get; init; } =
        "https://kantinemeny.azurewebsites.net/ukesmeny?lokasjon=Solheimsgaten5&dato=";
    public string SuppeUrl { get; init; } = 
        "https://kantinemeny.azurewebsites.net/ukesmenysuppe?lokasjon=Solheimsgaten5&dato=";
    public string XPath { get; init; } =
        "//body/div/div[@class='info boks']/div[@class='ukesmeny']/div[@class='ukedag']/div[@class='dagsinfo']/span";
    public string? OpenAiApiKey { get; init; }
}

public record Menu(string? MainMenu, string? SuppeMenu);

public class LunchTime(IMemoryCache cache, IOptions<LunchTimeOptions> options)
{
    private readonly LunchTimeOptions _options = options.Value;
    private readonly  Regex _localeRegex = new(@"^[a-zA-Z]{2,3}-[a-zA-Z]{2}$");

    public async Task<IResult> GetMenu(bool tomorrow, string? locale = null)
    {
        var dayIndex = (int)DateTime.UtcNow.DayOfWeek + (tomorrow ? 0 : -1);

        if (locale is not null && !_localeRegex.IsMatch(locale)) return TypedResults.BadRequest("Invalid locale, ex 'en-US' or 'nb-NO'.");
        
        if (dayIndex > 4) return TypedResults.UnprocessableEntity($"{(tomorrow ? "Tomorrow is" : "It's")} {DateTime.Now.AddDays(tomorrow ? 1 : 0).DayOfWeek}, dingus!");
        if (!string.IsNullOrWhiteSpace(locale) && string.IsNullOrWhiteSpace(_options.OpenAiApiKey)) return TypedResults.UnprocessableEntity("OpenAI API key is not set.");
        
        var menu = new Menu(
            await GetMainMenu(dayIndex, locale),
            await GetSuppeMenu(dayIndex, locale)
        );
        
        if (menu is (null, null)) return TypedResults.NoContent();

        return TypedResults.Ok(menu);
    }

    private async Task<string?> GetMainMenu(int dayIndex, string? locale) => await cache.GetOrCreateAsync($"mainMenu/{dayIndex}/{locale}", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
        var item = await GetDayMenu(_options.MenuUrl, dayIndex);
        return locale is not null ? await Translate(item, locale) : item;
    });

    private async Task<string?> GetSuppeMenu(int dayIndex, string? locale) => await cache.GetOrCreateAsync($"suppeMenu/{dayIndex}/{locale}", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
        var item = await GetDayMenu(_options.SuppeUrl, dayIndex);
        return locale is not null ? await Translate(item, locale) : item;
    });

    private async Task<string?> GetDayMenu(string url, int dayIndex)
    {
        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(url);
        var menuNodes = htmlDoc.DocumentNode.SelectNodes(_options.XPath);
        return menuNodes.Chunk(2)
            .Select(n => string.Join(" ", n.Select(nn => HttpUtility.HtmlDecode(nn.InnerText))))
            .ElementAtOrDefault(dayIndex)?
            .Replace("m/", "med ", StringComparison.OrdinalIgnoreCase);
    }
    
    private async Task<string?> Translate(string? text, string locale)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var prompt = $"Translate this menu item to the locale '{locale}'. Return only plain text.\n\n{text}";
        var api = new OpenAIAPI(_options.OpenAiApiKey);
        var result = await api.Completions.CreateCompletionAsync(prompt);
        return result.Completions.First().Text.Trim();
    }
}
