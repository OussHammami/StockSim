namespace StockSim.Web.Options;

public sealed class MarketFeedOptions
{
    public string BaseUrl { get; set; } = ""; 
    public string HubPath { get; set; } = "/hubs/quotes";
    public string HubUrl => string.IsNullOrWhiteSpace(BaseUrl) ? "" : $"{BaseUrl.TrimEnd('/')}{HubPath}";
}
