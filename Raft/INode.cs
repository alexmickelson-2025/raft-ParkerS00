using Raft;

namespace ClassLibrary;

public interface INode
{
    public int Id { get; set; }
    public bool Paused { get; set; }
    public Task CastVoteRPC(CastVoteData voteRequest);
    public Task RequestVoteRPC(RequestVoteData voteRequest);
    public void SendAppendEntriesRPC();
    public Task RequestAppendEntriesRPC(RequestAppendEntriesData request);
    public Task ConfirmAppendEntriesRPC(ConfirmAppendEntriesData request);
    public void RecieveClientCommand(string key, string value);
    public void Pause();
    public void UnPause();
}
