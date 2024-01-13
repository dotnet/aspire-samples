namespace BlazorPublicSite.Services;

public class FeedService(HttpClient httpClient)
{
    public async Task<string> GetFeedMessage()
    {
        var response = await httpClient.GetAsync("/feed");
        return await response.Content.ReadAsStringAsync();
    }
}
