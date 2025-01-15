using ClassLibrary;
using System.Timers;

namespace Raft;

public class Node : INode
{
    public Node(List<INode> otherNodes, int id)
    {
        State = State.Follower;
        Votes = 0;
        Term = 1;
        OtherNodes = otherNodes;
        Id = id;
    }

    public Node(State startingState, int id)
    {
        State = startingState;
        Votes = 0;
        Term = 1;
        OtherNodes = new List<INode>();
        Id = id;
    }

    public Node(int id)
    {
        State = State.Follower;
        Votes = 0;
        Term = 1;
        OtherNodes = new List<INode>();
        Id = id;
    }

    public int Id { get; set; }
    public int LeaderId { get; set; }
    public int Votes { get; set; }
    public int Term { get; set; }
    public System.Timers.Timer Timer { get; set; }
    public State State { get; set; }
    public List<INode> OtherNodes { get; set; }

    public int MajorityVote { get => OtherNodes.Count / 2 + 1; }
    public Task<bool> CastVoteRPC(int termId, bool vote)
    {
        if (vote && termId == Term)
        {
            Votes += 1;
            return Task.FromResult(false);
        }
        return Task.FromResult(false);
    }

    public void DetermineWinner()
    {
        if (Votes >= MajorityVote)
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
        Votes = 1;
        SendVoteRequestRPC();
        return;
    }

    public void StartElectionTimer()
    {
        Timer = new(Random.Shared.Next(150, 301));
        Timer.AutoReset = false;
        State = State.Candidate;
        Timer.Start();
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

    public Task<bool> SendVoteRequestRPC()
    {
        foreach (var node in OtherNodes)
        {
            node.RequestVoteRPC(Term, Id);
        }
        return Task.FromResult(true);
    }

    public async Task RequestVoteRPC(int termId, int candidateId)
    {
        if (Term > termId)
        {
            return;
        }

        var currentNode = OtherNodes.Where(x => x.Id == candidateId).FirstOrDefault();
        
        if (currentNode is not null)
        {
            await currentNode.CastVoteRPC(candidateId, true);
        }
    }
}
