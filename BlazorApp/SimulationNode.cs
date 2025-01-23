using ClassLibrary;
using Raft;

namespace BlazorApp;

public class SimulationNode : INode
{
    public readonly Node InnerNode;
    public SimulationNode(Node node)
    {
        this.InnerNode = node;
    }

    public int Id { get => ((INode)InnerNode).Id; set => ((INode)InnerNode).Id = value; }
    public int LeaderId { get => ((INode)InnerNode).LeaderId; set => ((INode)InnerNode).LeaderId = value; }
    public State State { get => ((INode)InnerNode).State; set => ((INode)InnerNode).State = value; }
    public int Votes { get => ((INode)InnerNode).Votes; set => ((INode)InnerNode).Votes = value; }
    public int Term { get => ((INode)InnerNode).Term; set => ((INode)InnerNode).Term = value; }
    public System.Timers.Timer Timer { get => ((INode)InnerNode).Timer; set => ((INode)InnerNode).Timer = value; }
    public Dictionary<int, int> CurrentTermVotes { get => ((INode)InnerNode).CurrentTermVotes; set => ((INode)InnerNode).CurrentTermVotes = value; }
    public List<INode> OtherNodes { get => ((INode)InnerNode).OtherNodes; set => ((INode)InnerNode).OtherNodes = value; }
    public DateTime StartTime { get => ((INode)InnerNode).StartTime; set => ((INode)InnerNode).StartTime = value; }
    public int NetworkDelay { get; set; }
    public int NextIndex { get => ((INode)InnerNode).NextIndex; set => ((INode)InnerNode).NextIndex = value; }
    public Dictionary<int, string> StateMachine { get => ((INode)InnerNode).StateMachine; set => ((INode)InnerNode).StateMachine = value; }

    public Task CastVoteRPC(int candidateId, bool vote)
    {
        return ((INode)InnerNode).CastVoteRPC(candidateId, vote);
    }

    public Task ConfirmAppendEntriesRPC(int term, int nextIndex)
    {
        return ((INode)InnerNode).ConfirmAppendEntriesRPC(term, nextIndex);
    }

    public void DetermineWinner()
    {
        ((INode)InnerNode).DetermineWinner();
    }

    public async Task RequestAppendEntriesRPC(int term, int leaderId, int prevLogIndex, int prevLogTerm, List<Log> entries, int leaderCommit)
    {
        await Task.Delay(NetworkDelay);
        await ((INode)InnerNode).RequestAppendEntriesRPC(term, leaderId, prevLogIndex, prevLogTerm, entries, leaderCommit);
    }

    public async Task RequestVoteRPC(int termId, int candidateId)
    {
        await Task.Delay(NetworkDelay);
        await ((INode)InnerNode).RequestVoteRPC(termId, candidateId);
    }

    public void SendAppendEntriesRPC(int termId, int nextIndex)
    {
        ((INode)InnerNode).SendAppendEntriesRPC(termId, nextIndex);
    }

    public void SendClientConfirmation()
    {
        ((INode)InnerNode).SendClientConfirmation();
    }

    public void StartElection()
    {
        ((INode)InnerNode).StartElection();
    }

    public void StartElectionTimer()
    {
        ((INode)InnerNode).StartElectionTimer();
    }
}
