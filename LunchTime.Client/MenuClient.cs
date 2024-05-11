using System.Net.Http.Json;
using LunchTime.Core.Models;
using Microsoft.Extensions.Options;

namespace LunchTime.Client;

public class MenuClient(HttpClient httpClient, IOptions<MenuClient.Options> options) : IMenuClient
{
    private readonly Options _options = options.Value;

    public async Task<MenuEntry[]> GetMenu(string? locale, CancellationToken cancellationToken)
    {
        var requestUri = $"{_options.MenuApiUrl}{(locale is not null ? $"?locale={locale}" : "")}";
        var response = await httpClient.GetAsync(
            requestUri,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MenuEntry[]>(cancellationToken: cancellationToken) ?? Array.Empty<MenuEntry>();
    }

    public async Task<MenuEntry> GetCurrentMenu(bool tomorrow, string? locale, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(
            $"{_options.MenuApiUrl}/{(tomorrow ? "tomorrow" : "")}{(locale is not null ? $"?locale={locale}" : "")}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MenuEntry>(cancellationToken: cancellationToken) ?? MenuEntry.Empty;
    }

    public async Task<MenuEntry> GetDayMenu(string day, string? locale, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(
            $"{_options.MenuApiUrl}/{day}{(locale is not null ? $"?locale={locale}" : "")}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MenuEntry>(cancellationToken: cancellationToken) ?? MenuEntry.Empty;
    }

    public class Options
    {
        public static string SectionName = "LunchTimeClient";
        public required string MenuApiUrl { get; init; }
    }
}
