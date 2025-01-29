using ClassLibrary;
using System.Timers;

namespace Raft;

public class Node : INode
{
    public Node(List<INode> otherNodes, int id, IClient client)
    {
        State = State.Follower;
        Votes = 0;
        Term = 1;
        NextIndex = 0;
        LeaderCommitIndex = 0;
        OtherNodes = otherNodes;
        Id = id;
        CurrentClient = client;
        StartElectionTimer();
    }

    public Node(List<INode> otherNodes)
    {
        State = State.Follower;
        Votes = 0;
        Term = 1;
        NextIndex = 0;
        LeaderCommitIndex = 0;
        OtherNodes = otherNodes;
        StartElectionTimer();
    }

    public Node(int id)
    {
        State = State.Follower;
        Votes = 0;
        Term = 1;
        NextIndex = 0;
        LeaderCommitIndex = 0;
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
    public int PreviousLogIndex { get; set; }
    public int PreviousLogTerm { get; set; }
    public bool Paused { get; set; }
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
    public IClient CurrentClient { get; set; }

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

    public void IncreaseCommitedLogs(int nextIndex, int termId)
    {
        if (LeaderCommitIndex == nextIndex)
        {
            return;
        }

        int numberOfUpToDateNodes = 1;
        foreach (var node in OtherNodes)
        {
            if (nextIndex == NextIndex && termId == Term)
            {
                numberOfUpToDateNodes++;
            }
        }
        if (numberOfUpToDateNodes >= MajorityVote && logs.Count > 0)
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
        IncreaseCommitedLogs(nextIndex, termId);
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
                        logsToSend.Add(logs[NextIndex - 1]);
                        PreviousLogIndex = nextIndex - 1;
                        PreviousLogTerm = Term;

                        node.RequestAppendEntriesRPC(Term, Id, PreviousLogIndex, PreviousLogTerm, logsToSend, LeaderCommitIndex);
                    }
                    else
                    {
                        List<Log> logsToSend = new();
                        FollowersNextIndex[node.Id] = NextIndex - nextIndex;
                        for (int i = logsToSend.Count; i < nextIndex; i++)
                        {
                            logsToSend.Add(logs[i]);
                            PreviousLogIndex = nextIndex - (i + 1);
                        }
                        PreviousLogTerm = Term;
                        node.RequestAppendEntriesRPC(Term, Id, PreviousLogIndex, PreviousLogTerm, logsToSend, LeaderCommitIndex);
                    }
                }
                else
                {
                    node.RequestAppendEntriesRPC(Term, Id, PreviousLogIndex, PreviousLogTerm, logs, LeaderCommitIndex);
                }
            }
        }
    }

    public async Task RequestAppendEntriesRPC(int term, int leaderId, int prevLogIndex, int prevLogTerm, List<Log> entries, int leaderCommit)
    {
        var currentLeader = OtherNodes.Where(x => x.Id == leaderId).FirstOrDefault();

        if (currentLeader is not null)
        {
            if (term >= Term && currentLeader.Id == leaderId)
            {
                StartElectionTimer();
                State = State.Follower;
                LeaderId = leaderId;
                Term = term;
                NextIndex = prevLogIndex + 1;

                if (prevLogIndex != NextIndex)
                {
                    foreach (var log in entries)
                    {
                        logs.Add(log);
                    }
                }
                if (leaderCommit > StateMachine.Count)
                {
                    var termMatch = logs.Select(x => x.Term == prevLogTerm).FirstOrDefault();
                    if (logs.Count > 0 && termMatch)
                    {
                        var indexMatch = logs.Contains(logs[prevLogIndex]);
                        if (indexMatch)
                        {
                            var command = logs[prevLogIndex].Command;
                            if (command is not null)
                            {
                                StateMachine[prevLogIndex] = command;
                            }
                        }
                    }
                }
                if (NextIndex != prevLogIndex)
                {
                    await currentLeader.ConfirmAppendEntriesRPC(Term, NextIndex);
                    return;
                }
            }
            await currentLeader.ConfirmAppendEntriesRPC(Term, NextIndex);
        }
    }

    public async Task ConfirmAppendEntriesRPC(int term, int nextIndex)
    {
        if (nextIndex == NextIndex)
        {
            IncreaseCommitedLogs(nextIndex, term);
        }
        else
        {
            if (nextIndex >= 0)
            {
                NextIndex--;
            }
        }
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
        CurrentClient.ReceiveNodeMessage();
        return;
    }

    public void Pause()
    {
        Paused = true;
        Timer.Stop();
        Timer.Dispose();
    }

    public void UnPause()
    {
        Paused = false;
        if (State == State.Leader)
        {
            BecomeLeader();
        }
        if (State == State.Follower)
        {
            StartElectionTimer();
        }
    }
}
