using Raft;

namespace ClassLibrary;

public interface INode
{
    public int Id { get; set; }
    public int LeaderId { get; set; }
    public State State { get; set; }
    public int Votes { get; set; }
    public int Term { get; set; }
    public System.Timers.Timer Timer { get; set; }
    public DateTime StartTime { get; set; }
    public Dictionary<int, int> CurrentTermVotes { get; set; }  
    public List<INode> OtherNodes { get; set; }
    public void StartElectionTimer();
    public void ResetElectionTimer();
    public void StartElection();
    public Task CastVoteRPC(int candidateId, bool vote);
    public Task RequestVoteRPC(int termId, int candidateId);
    public void SendAppendEntriesRPC(int termId);
    public Task RequestAppendEntriesRPC();
    public Task ConfirmAppendEntriesRPC();
    public void DetermineWinner();
}
