using System.Text.Json.Serialization;

namespace LunchTime.Core.Models;

public record MenuEntry([property: JsonConverter(typeof(JsonStringEnumConverter))]DayOfWeek? DayOfWeek, string? MainMenu, string? SuppeMenu, string? ImageUrl)
{
    public static MenuEntry Empty => new(null, null, null, null);
};
