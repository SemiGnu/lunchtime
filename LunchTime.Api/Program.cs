using LunchTime.Api;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.Configure<MenuService.Options>(builder.Configuration.GetSection(MenuService.Options.SectionName));
builder.Services.AddSingleton<IMenuService, MenuService>();
builder.Services.Configure<DoorleService.Options>(builder.Configuration.GetSection(DoorleService.Options.SectionName));
builder.Services.AddSingleton<DoorleService>();

var app = builder.Build();


app.MapGet("/", (
    [FromServices] IMenuService lunchTime,
    [FromQuery] string? locale = null
) => lunchTime.GetMenu(locale));

app.MapGet("/{dayOfWeek}", (
    [FromServices] IMenuService lunchTime,
    [FromRoute] string dayOfWeek,
    [FromQuery] string? locale = null
) => lunchTime.GetDayMenu(dayOfWeek, locale));

app.MapGet("/tomorrow", (
    [FromServices] IMenuService lunchTime,
    [FromQuery] string? locale = null
) => lunchTime.GetCurrentMenu(true, locale));

app.MapGet("/today", (
    [FromServices] IMenuService lunchTime,
    [FromQuery] string? locale = null
) => lunchTime.GetCurrentMenu(false, locale));

app.Run();
