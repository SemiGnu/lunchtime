using Cocona;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using System.Web;

var builder = CoconaApp.CreateBuilder();

builder.Services.AddHttpClient();

var app = builder.Build();

app.AddCommand("lunch", Menu.GetMenu).WithDescription("Get the menu, dawg.");

app.Run();

public record GetMenuCliParameters(
    [Option("tomorrow", Description = "Get menu for tomorrow")]
    bool Tomorrow = false
) : ICommandParameterSet;

public class Menu
{
    public static void GetMenu(GetMenuCliParameters parameters)
    {
        const string url = "https://kantinemeny.azurewebsites.net/ukesmeny?lokasjon=Solheimsgaten5&dato=";
        const string suppeUrl = "https://kantinemeny.azurewebsites.net/ukesmenysuppe?lokasjon=Solheimsgaten5&dato=";
        var menu = GetMenu(url);
        var suppeMenu = GetMenu(suppeUrl);
        var dayIndex = (int)DateTime.UtcNow.DayOfWeek + (parameters.Tomorrow ? 0 : -1);
        Console.WriteLine(dayIndex >= menu.Count
            ? $"{(parameters.Tomorrow ? "Tomorrow is" : "It's")} {DateTime.Now.AddDays(parameters.Tomorrow ? 1 : 0).DayOfWeek}, dingus!"
            : HttpUtility.HtmlDecode($"{menu[dayIndex]}\n{suppeMenu[dayIndex]}"));
    }

    private static List<string> GetMenu(string url)
    {
        var web = new HtmlWeb();
        var htmlDoc = web.Load(url);
        var menuNodes = htmlDoc.DocumentNode.SelectNodes("//body/div/div[@class='info boks']/div[@class='ukesmeny']/div[@class='ukedag']/div[@class='dagsinfo']/span");
        return menuNodes.Chunk(2).Select(n => string.Join(" ", n.Select(nn => nn.InnerText))).ToList();
    }
}
