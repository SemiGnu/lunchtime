using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using LunchTime.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenAI_API;

namespace LunchTime.Api;

public class MenuService(DoorleService doorleService, IMemoryCache cache, IOptions<MenuService.Options> options)
{
    private readonly Options _options = options.Value;
    private readonly Regex _localeRegex = new(@"^[a-zA-Z]{2,3}-[a-zA-Z]{2}$");
    private readonly Regex _stengtRegex = new(@"^\s*STENGT\s.*$");

    private readonly DayOfWeek[] _weekdays =
        { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };


    public Task<IResult> GetCurrentMenu(bool tomorrow, string? locale = null)
    {
        var dayIndex = DateTime.UtcNow.DayOfWeek + (tomorrow ? 1 : 0);

        return GetDayMenu(dayIndex, locale);
    }

    public async Task<IResult> GetDayMenu(string dayOfWeek, string? locale = null) =>
        Enum.TryParse(dayOfWeek, true, out DayOfWeek dow) ? await GetDayMenu(dow, locale) : TypedResults.NotFound();
    public async Task<IResult> GetDayMenu(DayOfWeek dayOfWeek, string? locale)
    {
        if (locale is not null && !_localeRegex.IsMatch(locale)) return TypedResults.BadRequest("Invalid locale, ex 'en-US' or 'nb-NO'.");

        var menus = await GetWeekMenu(locale);
        var menu = menus?.FirstOrDefault(m => m.DayOfWeek == dayOfWeek) ?? MenuEntry.Empty;
        
        if (menu == MenuEntry.Empty) return TypedResults.UnprocessableEntity($"No lunch on {dayOfWeek}, dingus!");

        return TypedResults.Ok(menu);
    }

    public async Task<IResult> GetMenu(string? locale = null)
    {
        if (locale is not null && !_localeRegex.IsMatch(locale)) return TypedResults.BadRequest("Invalid locale, ex 'en-US' or 'nb-NO'.");

        var menus = await GetWeekMenu(locale);
        
        if (menus?.All(m => m == MenuEntry.Empty) ?? true) return TypedResults.UnprocessableEntity("No lunch this week, dingus!");
        
        return TypedResults.Ok(menus);
    }
    

    private async Task<MenuEntry[]?> GetWeekMenu(string? locale = null)
    {
        var mainMenu = await GetCachedMenu(_options.MenuUrl);
        var suppeMenu = await GetCachedMenu(_options.SuppeUrl);
        if (mainMenu is null || suppeMenu is null) return null;
        var menuTasks = mainMenu.Zip3(suppeMenu, _weekdays).Select(async tuple =>
        {
            var (main, suppe, dayOfWeek) = tuple;
            if (main == string.Empty || _stengtRegex.IsMatch(main.ToUpper())) return MenuEntry.Empty;
            var imageUrl = doorleService.GetDoorleUrl(main);
            var localizedMain = await GetCachedTranslation(main, locale);
            var localizedSuppe = await GetCachedTranslation(suppe, locale);
            return new MenuEntry(dayOfWeek, localizedMain, localizedSuppe, imageUrl);
        }).ToArray();
        await Task.WhenAll(menuTasks);
        return menuTasks.Select(m => m.Result).ToArray();
    }
    
    private async Task<string[]?> GetCachedMenu(string url) => await cache.GetOrCreateAsync($"{url}", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
        return await GetRawMenu(url);
    });

    private async Task<string[]> GetRawMenu(string url)
    {
        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(url);
        var menuNodes = htmlDoc.DocumentNode.SelectNodes(_options.XPath);
        return menuNodes.Chunk(2)
            .Select(n => string.Join(" ", n.Select(nn => HttpUtility.HtmlDecode(nn.InnerText))))
            .Select(m => m.Replace("m/", "med ", StringComparison.OrdinalIgnoreCase).Trim())
            .ToArray();
    }
    
    private async Task<string?> GetCachedTranslation(string? text, string? locale) => await cache.GetOrCreateAsync($"{text}-{locale}", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
        return locale is null ? text : await Translate(text, locale);
    });
    
    private async Task<string?> Translate(string? text, string locale)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var prompt = $"Translate this menu item to the locale '{locale}'. Return only one single line of plain text.\n\n{text}";
        var api = new OpenAIAPI(_options.OpenAiApiKey);
        var result = await api.Completions.CreateCompletionAsync(prompt);
        return result.Completions.First().Text.Trim();
    }
    
    public class Options
    {
        public static string SectionName = "MenuService";
        public string MenuUrl { get; init; } =
            "https://kantinemeny.azurewebsites.net/ukesmeny?lokasjon=Solheimsgaten5&dato=";
        public string SuppeUrl { get; init; } = 
            "https://kantinemeny.azurewebsites.net/ukesmenysuppe?lokasjon=Solheimsgaten5&dato=";
        public string XPath { get; init; } =
            "//body/div/div[@class='info boks']/div[@class='ukesmeny']/div[@class='ukedag']/div[@class='dagsinfo']/span";
        public string? OpenAiApiKey { get; init; }
    }

}