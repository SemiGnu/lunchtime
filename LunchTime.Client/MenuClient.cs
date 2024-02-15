using System.Net.Http.Json;
using LunchTime.Core.Models;
using Microsoft.Extensions.Options;

namespace LunchTime.Client;

public class MenuClient(HttpClient httpClient, IOptions<MenuClient.Options> options)
{
    private readonly Options _options = options.Value;

    public async Task<Menu> GetMenuAsync(bool tomorrow, string? locale, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(
            $"{_options.MenuApiUrl}/{(tomorrow ? "tomorrow" : "")}{(locale is not null ? $"?locale={locale}" : "")}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Menu>(cancellationToken: cancellationToken) ?? new Menu(null, null);
    }

    public class Options
    {
        public required string MenuApiUrl { get; init; }
    }
}
