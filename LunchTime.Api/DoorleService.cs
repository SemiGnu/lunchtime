using Microsoft.Extensions.Options;

namespace LunchTime.Api;

public class DoorleService(IOptions<DoorleService.Options> options)
{
    private readonly string _apiUrl = options.Value.ApiUrl;
    
    public string GetDoorleUrl(string prompt)
    {
        return $"{_apiUrl}/{prompt.ToLower().Replace(' ','-')}.svg";
    }
    public class Options
    {
        public const string SectionName = "Doorle";
        public required string ApiUrl { get; init; }
    }
}