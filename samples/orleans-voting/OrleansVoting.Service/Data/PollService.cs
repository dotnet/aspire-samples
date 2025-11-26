using OrleansVoting;

namespace OrleansVoting.Data;

public sealed partial class PollService(IGrainFactory grainFactory)
{
    private IUserAgentGrain? _userAgentGrain;

    public void Initialize(string clientIp) =>
        _userAgentGrain = grainFactory.GetGrain<IUserAgentGrain>(clientIp);

    public Task<string> CreatePollAsync(string question, List<string> options) =>
        _userAgentGrain!.CreatePoll(new PollState
        {
            Question = question,
            Options = options.Select(o => (o, 0)).ToList()
        });
    
    public Task<(PollState Results, bool Voted)> GetPollResultsAsync(string pollId) =>
        _userAgentGrain!.GetPollResults(pollId);

    public Task<PollState> AddVoteAsync(string pollId, int optionId) =>
        _userAgentGrain!.AddVote(pollId, optionId);

    public async ValueTask<IAsyncDisposable> WatchPoll(string pollId, IPollWatcher watcherObject)
    {
        var pollGrain = grainFactory.GetGrain<IPollGrain>(pollId);
        var watcherReference = grainFactory.CreateObjectReference<IPollWatcher>(watcherObject);
        var result = new PollWatcherSubscription(watcherObject, pollGrain, watcherReference);

        await ValueTask.CompletedTask;
        
        return result;
    }
}
