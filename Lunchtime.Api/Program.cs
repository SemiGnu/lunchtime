using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


var menu = new LunchTime(new MemoryCache(new MemoryCacheOptions()), Options.Create(new LunchTimeOptions()));


app.MapGet("/", (bool tomorrow = false) => menu.GetMenu(tomorrow))
    .Produces<Menu>()
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status422UnprocessableEntity)
    .WithName("GetMenu")
    .WithOpenApi();

app.Run();

public record LunchTimeOptions(
    string MenuUrl = "https://kantinemeny.azurewebsites.net/ukesmeny?lokasjon=Solheimsgaten5&dato=",
    string SuppeUrl = "https://kantinemeny.azurewebsites.net/ukesmenysuppe?lokasjon=Solheimsgaten5&dato=",
    string XPath = "//body/div/div[@class='info boks']/div[@class='ukesmeny']/div[@class='ukedag']/div[@class='dagsinfo']/span"
);

public record Menu(string? MainMenu, string? SuppeMenu);

public class LunchTime(IMemoryCache cache, IOptions<LunchTimeOptions> options)
{
    private readonly LunchTimeOptions _options = options.Value;

    public async Task<IResult> GetMenu(bool tomorrow)
    {
        var dayIndex = (int)DateTime.UtcNow.DayOfWeek + (tomorrow ? 0 : -1);
        var menu = new Menu(
            await GetMainMenu(dayIndex),
            await GetSuppeMenu(dayIndex)
        );
        if (menu is (null, null))
        {
            return dayIndex > 4
                ? TypedResults.UnprocessableEntity($"{(tomorrow ? "Tomorrow is" : "It's")} {DateTime.Now.AddDays(tomorrow ? 1 : 0).DayOfWeek}, dingus!")
                : TypedResults.NoContent();
        }

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
