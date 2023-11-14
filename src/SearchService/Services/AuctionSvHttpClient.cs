using MongoDB.Entities;

namespace SearchService;
public class AuctionSvHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public AuctionSvHttpClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<List<Item>> GetItemsForSearchDb()
    {
      // returns the latest date on which an auction is updated 
    var lastUpdated = await DB.Find<Item, string>()
        .Sort(x => x.Descending(i => i.UpdatedAt))
        .Project(i => i.UpdatedAt.ToString())
        .ExecuteFirstAsync();

    return await _httpClient.GetFromJsonAsync<List<Item>>
            (_config["AuctionServiceUrl"] + "/api/auctions?date=" + lastUpdated);
    }
}
