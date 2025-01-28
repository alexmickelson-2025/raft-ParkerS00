using Raft;

namespace ClassLibrary;

public interface INode
{
    public int Id { get; set; }
    public State State { get; set; }
    public int Term { get; set; }
    public int NextIndex { get; set; }
    public bool Paused { get; set; }
    public System.Timers.Timer Timer { get; set; }
    public DateTime StartTime { get; set; }
    public Dictionary<int, int> CurrentTermVotes { get; set; }  
    public Dictionary<int, string> StateMachine { get; set; }
    public List<INode> OtherNodes { get; set; }
    public List<Log> logs { get; set; }
    public void StartElectionTimer();
    public void StartElection();
    public Task CastVoteRPC(int candidateId, bool vote);
    public Task RequestVoteRPC(int termId, int candidateId);
    public void SendAppendEntriesRPC(int termId, int nextIndex);
    public Task RequestAppendEntriesRPC(int term, int leaderId, int prevLogIndex, int prevLogTerm, List<Log> entries, int leaderCommit);
    public Task ConfirmAppendEntriesRPC(int term, int nextIndex);
    public void DetermineWinner();
    public void RecieveClientCommand(string command);
    public void SendClientConfirmation();
    public void Pause();
    public void UnPause();
}
