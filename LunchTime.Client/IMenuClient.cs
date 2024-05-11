using LunchTime.Core.Models;

namespace LunchTime.Client;

public interface IMenuClient
{
    Task<MenuEntry[]> GetMenu(string? locale, CancellationToken cancellationToken);
    Task<MenuEntry> GetCurrentMenu(bool tomorrow, string? locale, CancellationToken cancellationToken);
    Task<MenuEntry> GetDayMenu(string day, string? locale, CancellationToken cancellationToken);
}