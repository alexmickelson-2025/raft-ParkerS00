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
        NextIndex = 0;
        OtherNodes = otherNodes;
        Id = id;
        StartElectionTimer();
    }

    public Node(int id)
    {
        State = State.Follower;
        Votes = 0;
        Term = 1;
        NextIndex = 0;
        OtherNodes = new List<INode>();
        Id = id;
        StartElectionTimer();
    }

    public int Id { get; set; }
    public int LeaderId { get; set; }
    public int Votes { get; set; }
    public int Term { get; set; }
    public int NextIndex { get; set; }
    public int LeaderCommitIndex { get; set; }
    public System.Timers.Timer Timer { get; set; } = new();
    public DateTime StartTime { get; set; }
    public State State { get; set; }
    public List<INode> OtherNodes { get; set; }
    public List<Log> logs { get; set; } = new();
    public Dictionary<int, int> CurrentTermVotes { get; set; } = new();
    public Dictionary<int, int> FollowersNextIndex { get; set; } = new();
    public Dictionary<int, string> StateMachine { get; set; } = new();
    public int MajorityVote { get => (OtherNodes.Count / 2) + 1; }
    public int MaxDelay { get; set; } = 300;
    public int MinDelay { get; set; } = 150;
    public int LeaderDelay { get; set; } = 50;

    public void StartElection()
    {
        if (State == State.Leader)
        {
            return;
        }
        Timer.Stop();
        Timer.Dispose();
        State = State.Candidate;
        Term += 1;
        Votes = 1;
        CurrentTermVotes[Term] = Id;
        SendVoteRequestRPC();
        StartElectionTimer();
    }

    public void StartElectionTimer()
    {
        if (State == State.Leader)
        {
            return;
        }
        Timer.Stop();
        Timer.Dispose();
        Timer = new(Random.Shared.Next(MinDelay, MaxDelay));
        Timer.AutoReset = false;
        StartTime = DateTime.Now;
        Timer.Elapsed += (s, e) =>
        {
            StartElection();
        };
        Timer.Start();
    }

    public void BecomeLeader()
    {
        State = State.Leader;
        LeaderId = Id;
        StartHeartbeatTimer();
        SendAppendEntriesRPC(Term, NextIndex);
    }

    public void StartHeartbeatTimer()
    {
        Timer.Stop();
        Timer.Dispose();
        Timer = new(LeaderDelay);
        StartTime = DateTime.Now;
        Timer.Elapsed += (s, e) => { SendAppendEntriesRPC(Term, NextIndex); };
        Timer.Start();
    }

    public void DetermineWinner()
    {
        if (Votes >= MajorityVote && State == State.Candidate)
        {
            BecomeLeader();
        }
    }

    public void IncreaseCommitedLogs()
    {
        if (LeaderCommitIndex == NextIndex)
        {
            return;
        }

        int numberOfUpToDateNodes = 1;
        foreach (var node in OtherNodes)
        {
            if (node.NextIndex == NextIndex)
            {
                numberOfUpToDateNodes++;
            }
        }
        if (numberOfUpToDateNodes >= MajorityVote)
        {
            LeaderCommitIndex = NextIndex;
            var command = logs[LeaderCommitIndex - 1].Command;
            if (command is not null)
            {
                StateMachine[LeaderCommitIndex] = command;
                SendClientConfirmation();
            }
        }
    }

    public void SendAppendEntriesRPC(int termId, int nextIndex)
    {
        IncreaseCommitedLogs();
        StartTime = DateTime.Now;
        if (Term >= termId && State == State.Leader)
        {
            foreach (var node in OtherNodes)
            {
                InitializeLogs(node);
                if (logs.Count >= 1)
                {
                    if (FollowersNextIndex[node.Id] == nextIndex)
                    {
                        List<Log> logsToSend = new();
                        FollowersNextIndex[node.Id] = nextIndex;
                        logsToSend.Add(logs[nextIndex - 1]);
                        node.LeaderId = Id;
                        node.State = State.Follower;
                        node.NextIndex = nextIndex;
                        node.RequestAppendEntriesRPC(Term, Id, 0, 0, logsToSend, LeaderCommitIndex);
                    }
                    else
                    {
                        List<Log> logsToSend = new();
                        var logsBehind = nextIndex - node.NextIndex;
                        FollowersNextIndex[node.Id] = nextIndex - logsBehind;
                        for (int i = node.NextIndex; i < nextIndex; i++)
                        {
                            logsToSend.Add(logs[i]);
                        }
                        node.LeaderId = Id;
                        node.State = State.Follower;
                        node.NextIndex = nextIndex;
                        node.RequestAppendEntriesRPC(Term, Id, 0, 0, logsToSend, LeaderCommitIndex);
                    }
                }
                else
                {
                    node.LeaderId = Id;
                    node.State = State.Follower;
                    node.NextIndex = nextIndex;
                    node.RequestAppendEntriesRPC(Term, Id, 0, 0, logs, 0);
                }
            }
        }
    }

    public async Task RequestAppendEntriesRPC(int term, int leaderId, int prevLogIndex, int prevLogTerm, List<Log> entries, int leaderCommit)
    {
        var currentLeader = OtherNodes.Where(x => x.Id == leaderId).FirstOrDefault();

        if (currentLeader is not null)
        {
            if (currentLeader.Term >= term && currentLeader.Id == leaderId)
            {
                StartElectionTimer();
                State = State.Follower;
                LeaderId = leaderId;
                Term = term;
                foreach (var log in entries)
                {
                    logs.Add(log);
                }
                await currentLeader.ConfirmAppendEntriesRPC(Term, NextIndex);
            }
        }
    }

    public async Task ConfirmAppendEntriesRPC(int term, int nextIndex)
    {
        IncreaseCommitedLogs();
        await Task.CompletedTask;
    }

    public void InitializeLogs(INode node)
    {
        if (!FollowersNextIndex.ContainsKey(node.Id))
        {
            FollowersNextIndex[node.Id] = 0;
        }
    }

    public void SendVoteRequestRPC()
    {
        if (State == State.Candidate)
        {
            foreach (var node in OtherNodes)
            {
                node.RequestVoteRPC(Term, Id);
            }
        }
    }

    public async Task RequestVoteRPC(int termId, int candidateId)
    {
        if (Term > termId)
        {
            return;
        }

        var candidateNode = OtherNodes.Where(x => x.Id == candidateId).FirstOrDefault();

        if (candidateNode is not null)
        {
            if (!CurrentTermVotes.ContainsKey(termId))
            {
                CurrentTermVotes[termId] = candidateId;
                await candidateNode.CastVoteRPC(termId, true);
            }
            await candidateNode.CastVoteRPC(termId, false);
        }
    }
    public async Task CastVoteRPC(int termId, bool vote)
    {
        if (vote && termId == Term)
        {
            Votes += 1;
        }
        if (Votes >= MajorityVote)
        {
            DetermineWinner();
        }
        await Task.CompletedTask;
    }

    public void RecieveClientCommand(string command)
    {
        Log newLog = new Log(Term, command);
        logs.Add(newLog);
        NextIndex = logs.Count;
    }

    public void SendClientConfirmation()
    {
        return;
    }
}
