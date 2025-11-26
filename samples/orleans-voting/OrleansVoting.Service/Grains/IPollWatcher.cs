namespace OrleansVoting;

public interface IPollWatcher : IGrainObserver
{
    void OnPollUpdated(PollState state);
}
