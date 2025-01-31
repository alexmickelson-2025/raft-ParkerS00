using ClassLibrary;
using System.Timers;

namespace Raft;

public class Node : INode
{
    public Node(List<INode> otherNodes, int id, IClient client)
    {
        State = State.Follower;
        Votes = 0;
        Term = 0;
        CommitIndex = 0;
        OtherNodes = otherNodes;
        Id = id;
        CurrentClient = client;
        StartElectionTimer();
    }

    public Node(List<INode> otherNodes)
    {
        State = State.Follower;
        Votes = 0;
        Term = 0;
        CommitIndex = 0;
        OtherNodes = otherNodes;
        StartElectionTimer();
    }

    public Node(int id)
    {
        State = State.Follower;
        Votes = 0;
        Term = 0;
        CommitIndex = 0;
        OtherNodes = new List<INode>();
        Id = id;
        StartElectionTimer();
    }

    public int Id { get; set; }
    public int LeaderId { get; set; }
    public int Votes { get; set; }
    public int Term { get; set; }
    public int NextIndex { get => logs.Count; }
    public int CommitIndex { get; set; }
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
    public Dictionary<string, string> StateMachine { get; set; } = new();
    public Dictionary<int, int> LogsReplicated { get; set; } = new();
    public int MajorityVote { get => ((OtherNodes.Count + 1) / 2) + 1; }
    public int MaxDelay { get; set; } = 300;
    public int MinDelay { get; set; } = 150;
    public int LeaderDelay { get; set; } = 50;
    public IClient CurrentClient { get; set; }

    public void StartElection()
    {
        if (Paused || State == State.Leader)
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
        if (Paused || State == State.Leader)
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
        InitializeLogs();
        SendAppendEntriesRPC();
    }

    public void StartHeartbeatTimer()
    {
        Timer.Stop();
        Timer.Dispose();
        Timer = new(LeaderDelay);
        StartTime = DateTime.Now;
        Timer.Elapsed += (s, e) => { SendAppendEntriesRPC(); };
        Timer.Start();
    }

    public void DetermineWinner()
    {
        if (Votes == MajorityVote && State == State.Candidate)
        {
            BecomeLeader();
        }
    }

    public void IncreaseCommitedLogs(int nextIndexToCommit)
    {
        if (CommitIndex == NextIndex || State == State.Follower || State == State.Candidate)
        {
            return;
        }

        for (int i = CommitIndex; i < nextIndexToCommit - 1; i++)
        {
            var currentLog = logs[i];
            StateMachine[currentLog.Key] = currentLog.Value;
        }
        CommitIndex = nextIndexToCommit - 1;
        SendClientConfirmation(true);
    }

    public void SendAppendEntriesRPC()
    {
        if (Paused || State == State.Follower)
        {
            return;
        }
        StartTime = DateTime.Now;

        foreach (var node in OtherNodes)
        {
            if (logs.Count >= 1)
            {
                PreviousLogIndex = NextIndex - 1;
                PreviousLogTerm = logs[NextIndex - 1].Term; 
                node.RequestAppendEntriesRPC(Term, Id, PreviousLogIndex, PreviousLogTerm, logs, CommitIndex);
            }
            else
            {
                node.RequestAppendEntriesRPC(Term, Id, PreviousLogIndex, PreviousLogTerm, logs, CommitIndex);
            }
        }
    }

    public async Task RequestAppendEntriesRPC(int term, int leaderId, int prevLogIndex, int prevLogTerm, List<Log> entries, int leaderCommit)
    {
        if (Paused)
        {
            return;
        }

        if (term > Term && (State == State.Candidate || State == State.Leader))
        {
            State = State.Follower;
        }

        var currentLeader = OtherNodes.Where(x => x.Id == leaderId).FirstOrDefault();

        if (currentLeader is not null)
        {
            StartElectionTimer();
            State = State.Follower;
            LeaderId = leaderId;
            Term = term;

            if (leaderCommit > CommitIndex)
            {
                IncreaseFollowerCommitedLogs(leaderCommit);
            }

            if (logs.Count < 1 && prevLogIndex == 0 && (prevLogTerm == 0 || prevLogTerm == 1))
            {
                logs.AddRange(entries);
                PreviousLogIndex = 0;
                PreviousLogTerm = prevLogTerm;
                await currentLeader.ConfirmAppendEntriesRPC(Term, NextIndex, true, Id);
            }
            else if (logs.Count > prevLogIndex && logs[prevLogIndex] is not null && logs.Last().Term == prevLogTerm) 
            {
                logs.AddRange(entries);
                PreviousLogIndex = prevLogIndex;
                PreviousLogTerm = prevLogTerm;
                await currentLeader.ConfirmAppendEntriesRPC(Term, NextIndex, true, Id);
            }
            else if (logs.Count > 1 && logs[prevLogIndex + 1] is not null)
            {
                var logsToRemove = logs.Count - prevLogIndex;
                logs.RemoveRange(NextIndex - 1, logsToRemove);

                logs.AddRange(entries);
                await currentLeader.ConfirmAppendEntriesRPC(Term, NextIndex, true, Id);
            }
            else
            {
                await currentLeader.ConfirmAppendEntriesRPC(Term, NextIndex, false, Id);
            }
        }
    }

    public async Task ConfirmAppendEntriesRPC(int term, int nextIndex, bool status, int id)
    {
        if (Paused || State == State.Follower)
        {
            return;
        }

        if (term > Term)
        {
            State = State.Follower;
            StartElectionTimer();
        }

        if (status == false)
        {
            FollowersNextIndex[Id] = nextIndex--;
        }

        if (status == true)
        {
            FollowersNextIndex[Id] = NextIndex;
            LogsReplicated[nextIndex]++;

            if (LogsReplicated[nextIndex] == MajorityVote)
            {
                IncreaseCommitedLogs(LogsReplicated[nextIndex]);
            }
        }

        await Task.CompletedTask;
    }

    public void IncreaseFollowerCommitedLogs(int nextIndexToCommit)
    {
        for (int i = CommitIndex; i < nextIndexToCommit; i++)
        {
            var currentLog = logs[i];
            StateMachine[currentLog.Key] = currentLog.Value;
        }
        CommitIndex = nextIndexToCommit - 1;
    }

    public void InitializeLogs()
    {
        foreach (var node in OtherNodes)
        {
            FollowersNextIndex[node.Id] = NextIndex;
        }
    }

    public void SendVoteRequestRPC()
    {
        if (Paused)
        {
            return;
        }
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
        if (Paused)
        {
            return;
        }
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
        if (Paused)
        {
            return;
        }
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

    public void RecieveClientCommand(string key, string value)
    {
        Log newLog = new Log(Term, key, value);
        logs.Add(newLog);

        LogsReplicated.Add(NextIndex, 1);
    }

    public bool SendClientConfirmation(bool state)
    {
        CurrentClient.ReceiveNodeMessage();
        return state;
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
