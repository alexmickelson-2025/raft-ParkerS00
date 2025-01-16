﻿using ClassLibrary;
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
        StartElectionTimer();
    }

    public Node(State startingState, int id)
    {
        State = startingState;
        Votes = 0;
        Term = 1;
        OtherNodes = new List<INode>();
        Id = id;
        StartElectionTimer();
    }

    public Node(int id)
    {
        State = State.Follower;
        Votes = 0;
        Term = 1;
        OtherNodes = new List<INode>();
        Id = id;
        StartElectionTimer();
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

    public void StartElection()
    {
        Timer.Dispose();
        StartElectionTimer();
        State = State.Candidate;
        Term += 1;
        Votes = 1;
        SendVoteRequestRPC();
        return;
    }

    public void StartElectionTimer()
    {
        Timer = new(Random.Shared.Next(150, 300));
        Timer.Elapsed += (s, e) => { StartElection(); };
        Timer.AutoReset = false;
        Timer.Start();
    }

    public void ResetElectionTimer()
    {
        Timer = new(Random.Shared.Next(150, 301));
        Timer.AutoReset = false;
        Timer.Start();
    }

    public void BecomeLeader()
    {
        Timer.Dispose();
        State = State.Leader;
        SendAppendEntriesRPC(Term);
        StartHeartbeatTimer();
    }

    public void StartHeartbeatTimer()
    {
        Timer = new(50);
        Timer.Elapsed += (s, e) => { SendAppendEntriesRPC(Term); }; 
        Timer.Start();
    }

    public void DetermineWinner()
    {
        if (Votes >= MajorityVote)
        {
            BecomeLeader();
        }
        else
        {
            StartElection();
        }
    }

    public void SendAppendEntriesRPC(int termId)
    {
        if (Term >= termId)
        {
            foreach (var node in OtherNodes)
            {
                node.LeaderId = Id;
                node.RequestAppendEntriesRPC();
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
                State = State.Follower;
                ResetElectionTimer();
            }
        }
    }

    public async Task ConfirmAppendEntriesRPC()
    {
        await Task.CompletedTask;
    }

    public void SendVoteRequestRPC()
    {
        foreach (var node in OtherNodes)
        {
            node.RequestVoteRPC(Term, Id);
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
