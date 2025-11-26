using Grains;
using Orleans.Runtime;
using Orleans.Utilities;
using OrleansVoting;

namespace VotingData;

public class PollGrain(
    [PersistentState(stateName: "pollState", storageName: "votes")] IPersistentState<PollState> state,
    ILogger<ObserverManager<IPollWatcher>> pollLogger) : Grain, IPollGrain
{
    private readonly ObserverManager<IPollWatcher> _pollWatchers = new(TimeSpan.FromMinutes(1), pollLogger);

    public Task<PollState> GetCurrentResults() => Task.FromResult(state.State);

    public async Task CreatePoll(PollState initialState)
    {
        // Set the state and persist it
        state.State = initialState;
        await state.WriteStateAsync();
    }

    public async Task<PollState> AddVote(int optionId)
    {
        // Perform input validation
        var options = state.State.Options;
        if (optionId < 0 || optionId >= options.Count)
        {
            throw new KeyNotFoundException($"Invalid option {optionId}");
        }

        // Add the vote & persist the updated state.
        var (option, votes) = options[optionId];
        options[optionId] = (option, votes + 1);
        await state.WriteStateAsync();

        // Notify the watchers.
        _pollWatchers.Notify(watcher => watcher.OnPollUpdated(state.State));
        return state.State;
    }

    public Task StartWatching(IPollWatcher watcher)
    {
        _pollWatchers.Subscribe(watcher, watcher);
        return Task.CompletedTask;
    }

    public Task StopWatching(IPollWatcher watcher)
    {
        _pollWatchers.Unsubscribe(watcher);
        return Task.CompletedTask;
    }
}
