using ClassLibrary;
using System.Timers;
using System.Xml.Linq;

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
    public Dictionary<int, int> CurrentTermVotes { get; set; } = new();
    public int MajorityVote { get => OtherNodes.Count / 2 + 1; }

    public async Task StartElection()
    {
        StartElectionTimer();
        Term += 1;
        Votes = 1;
        await SendVoteRequestRPC();
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

    public async Task DetermineWinner()
    {
        if (Votes >= MajorityVote)
        {
            State = State.Leader;

            await SendAppendEntriesRPC(Term);
        }
        else
        {
            await StartElection();
        }
    }

    public async Task SendAppendEntriesRPC(int termId)
    {
        if (Term >= termId)
        {
            foreach (var node in OtherNodes)
            {
                node.LeaderId = Id;
                await node.RequestAppendEntriesRPC();
            }
        }
    }

    public async Task RequestAppendEntriesRPC()
    {
        var currentNode = OtherNodes.Where(x => x.Id == LeaderId).FirstOrDefault();

        if (currentNode is not null)
        {
            if (currentNode.Term >= Term)
            {
                await currentNode.ConfirmAppendEntriesRPC();
            }
        }
    }

    public async Task ConfirmAppendEntriesRPC()
    {
        await Task.CompletedTask;
    }

    public async Task SendVoteRequestRPC()
    {
        foreach (var node in OtherNodes)
        {
            await node.RequestVoteRPC(Term, Id);
        }
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
            if (!CurrentTermVotes.ContainsKey(currentNode.Id))
            {
                CurrentTermVotes.Add(currentNode.Id, termId);
                await currentNode.CastVoteRPC(candidateId, true);
            }
            await currentNode.CastVoteRPC(candidateId, false);
        }
    }
    public async Task CastVoteRPC(int termId, bool vote)
    {
        if (vote && termId == Term)
        {
            Votes += 1;
            await Task.FromResult(false);
        }
        await Task.FromResult(false);
    }
}
