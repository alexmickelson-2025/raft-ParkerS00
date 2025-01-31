using Raft;

namespace ClassLibrary;

public interface INode
{
    public int Id { get; set; }
    public bool Paused { get; set; }
    public Task CastVoteRPC(int candidateId, bool vote);
    public Task RequestVoteRPC(int termId, int candidateId);
    public void SendAppendEntriesRPC();
    public Task RequestAppendEntriesRPC(int term, int leaderId, int prevLogIndex, int prevLogTerm, List<Log> entries, int leaderCommit);
    public Task ConfirmAppendEntriesRPC(int term, int nextIndex, bool status, int id);
    public void RecieveClientCommand(string key, string value);
    public void Pause();
    public void UnPause();
}
