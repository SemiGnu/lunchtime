using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;using Microsoft.VisualBasic.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.Configure<LunchTimeOptions>(builder.Configuration.GetSection("LunchTime"));
builder.Services.AddSingleton<LunchTime>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", ([FromServices] LunchTime lunchTime, [FromQuery] bool tomorrow = false) => lunchTime.GetMenu(tomorrow))
    .Produces<Menu>()
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status422UnprocessableEntity)
    .WithName("GetMenu")
    .WithOpenApi();

app.Run();

public class LunchTimeOptions
{
    public string MenuUrl { get; init; } =
        "https://kantinemeny.azurewebsites.net/ukesmeny?lokasjon=Solheimsgaten5&dato=";
    public string SuppeUrl { get; init; } = 
        "https://kantinemeny.azurewebsites.net/ukesmenysuppe?lokasjon=Solheimsgaten5&dato=";
    public string XPath { get; init; } =
        "//body/div/div[@class='info boks']/div[@class='ukesmeny']/div[@class='ukedag']/div[@class='dagsinfo']/span";
}

public record Menu(string? MainMenu, string? SuppeMenu);

public class LunchTime(IMemoryCache cache, IOptions<LunchTimeOptions> options)
{
    private readonly LunchTimeOptions _options = options.Value;

    public async Task<IResult> GetMenu(bool tomorrow)
    {
        var dayIndex = (int)DateTime.UtcNow.DayOfWeek + (tomorrow ? 0 : -1);
        
        if (dayIndex > 4) return TypedResults.UnprocessableEntity($"{(tomorrow ? "Tomorrow is" : "It's")} {DateTime.Now.AddDays(tomorrow ? 1 : 0).DayOfWeek}, dingus!");
        
        var menu = new Menu(
            await GetMainMenu(dayIndex),
            await GetSuppeMenu(dayIndex)
        );
        
        if (menu is (null, null)) return TypedResults.NoContent();

        return TypedResults.Ok(menu);
    }

    private async Task<string?> GetMainMenu(int dayIndex) => await cache.GetOrCreateAsync($"mainMenu/{dayIndex}", entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
        return GetDayMenu(_options.MenuUrl, dayIndex);
    });

    private async Task<string?> GetSuppeMenu(int dayIndex) => await cache.GetOrCreateAsync($"suppeMenu/{dayIndex}", entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8);
        return GetDayMenu(_options.SuppeUrl, dayIndex);
    });

    private async Task<string?> GetDayMenu(string url, int dayIndex)
    {
        var web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(url);
        var menuNodes = htmlDoc.DocumentNode.SelectNodes(_options.XPath);
        return menuNodes.Chunk(2).Select(n => string.Join(" ", n.Select(nn => nn.InnerText))).ElementAtOrDefault(dayIndex);
    }
}
