using ClassLibrary;
using System.Timers;

namespace Raft;

public class Node : INode
{
    public Node(List<INode> otherNodes)
    {
        State = State.Follower;
        Votes = 0;
        Term = 1;
        OtherNodes = otherNodes;
        Id = OtherNodes.Count + 1;
    }

    public Node(State startingState)
    {
        State = startingState;
        Votes = 0;
        Term = 1;
        OtherNodes = new List<INode>();
        Id = 1;
    }

    public Node()
    {
        State = State.Follower;
        Votes = 0;
        Term = 1;
        OtherNodes = new List<INode>();
        Id = 1;
    }

    public int Id { get; set; }
    public int LeaderId { get; set; }
    public int Votes { get; set; }
    public int Term { get; set; }
    public System.Timers.Timer Timer { get; set; }
    public State State { get; set; }
    public List<INode> OtherNodes { get; set; }

    public void CastVote()
    {
        if (State == State.Candidate)
        {
            Votes += 1;
            return;
        }
    }

    public void DetermineWinner()
    {
        if (Votes >= (OtherNodes.Count / 2) + 1)
        {
            State = State.Leader;
            
            SendAppendEntriesRPC();
        }
        else
        {
            StartElection();
        }
    }

    public void StartElection()
    {
        StartElectionTimer();
        Term += 1;
        CastVote();
        return;
    }

    public void StartElectionTimer()
    {
        Timer = new(Random.Shared.Next(150, 301));
        Timer.Start();
        Timer.AutoReset = false;
        State = State.Candidate;
        Votes = 0;
    }

    public Task<bool> SendAppendEntriesRPC()
    {
        foreach (var node in OtherNodes)
        {
            node.RecieveAppendEntriesRPC();
            node.LeaderId = Id;
        }
        return Task.FromResult(true);
    }

    public Task<bool> RecieveAppendEntriesRPC()
    {
        throw new NotImplementedException();
    }
}
