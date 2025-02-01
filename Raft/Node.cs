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
    public int MaxDelay { get; set; } = 500;
    public int MinDelay { get; set; } = 250;
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
        if (State != State.Leader)
        {
            return;
        }

        Timer.Stop();
        Timer.Dispose();
        Timer = new(LeaderDelay);
        StartTime = DateTime.Now;
        Timer.Elapsed += (s, e) => { SendAppendEntriesRPC(); StartHeartbeatTimer(); };
        Timer.AutoReset = false;
        Timer.Start();
    }

    public void DetermineWinner()
    {
        if (Votes >= MajorityVote && State == State.Candidate)
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
                var thing = FollowersNextIndex[node.Id];
                var thing2 = (thing > 0 && thing - 1 < logs.Count()) ? logs[thing - 1].Term : 0;

                var logsToSend = new List<Log>();
                for (int i = thing; i < NextIndex; i++)
                {
                    logsToSend.Add(logs[i]);
                }

                node.RequestAppendEntriesRPC(new RequestAppendEntriesData(Term, Id, thing, thing2, logsToSend, CommitIndex));
                continue;
            }
            else
            {
                var thing = FollowersNextIndex[node.Id];
                var thing2 = (thing > 0 && thing - 1 < logs.Count()) ? logs[thing - 1].Term : 0;
                node.RequestAppendEntriesRPC(new RequestAppendEntriesData(Term, Id, thing, thing2, [], CommitIndex));
                continue;

            }
        }
    }

    public async Task RequestAppendEntriesRPC(RequestAppendEntriesData request)
    {
        await Task.CompletedTask;
        if (Paused)
        {
            return;
        }

        if (request.Term > Term && (State == State.Candidate || State == State.Leader))
        {
            State = State.Follower;
        }

        var currentLeader = OtherNodes.Where(x => x.Id == request.LeaderId).FirstOrDefault();

        var currentNode = Id;

        StartElectionTimer();
        State = State.Follower;
        LeaderId = request.LeaderId;
        Term = request.Term;
        if (currentLeader is not null)
        {

            if (request.LeaderCommit > CommitIndex)
            {
                IncreaseFollowerCommitedLogs(request.LeaderCommit);
            }

            // Case for the first log
            if (logs.Count < 1 && request.PrevLogIndex == 0 && request.PrevLogTerm == 0)
            {
                logs.AddRange(request.Entries);
                PreviousLogIndex = 0;
                PreviousLogTerm = request.PrevLogTerm;
                currentLeader.ConfirmAppendEntriesRPC(new(Term, NextIndex, true, Id));
            }
            // Case for if they are 1 log behind
            else if (logs.Count() == request.PrevLogIndex  && logs.Count() > 0 && logs.Last().Term == request.PrevLogTerm)
            {
                logs.AddRange(request.Entries);
                PreviousLogIndex = request.PrevLogIndex;
                PreviousLogTerm = request.PrevLogTerm;
                 currentLeader.ConfirmAppendEntriesRPC(new(Term, NextIndex, true, Id));
            }
            // Case for if they are ahead on logs
            else if (logs.Count > 1 && logs[request.PrevLogIndex + 1] is not null)
            {
                var logsToRemove = logs.Count - request.PrevLogIndex;
                logs.RemoveRange(request.PrevLogIndex - 1, logsToRemove);

                logs.AddRange(request.Entries);
                 currentLeader.ConfirmAppendEntriesRPC(new(Term, NextIndex, true, Id));
            }
            // Case for if they are behind on logs
            else
            {
                 currentLeader.ConfirmAppendEntriesRPC(new(Term, NextIndex, false, Id));
            }
        }
    }

    public async Task ConfirmAppendEntriesRPC(ConfirmAppendEntriesData request)
    {
        if (Paused)
        {
            return;
        }

        if (request.Term > Term)
        {
            State = State.Follower;
            StartElectionTimer();
        }

        if (request.Status == false)
        {
            if (FollowersNextIndex[request.Id] > 0)
            {
                FollowersNextIndex[request.Id]--;
            }
        }

        if (request.Status == true)
        {
            FollowersNextIndex[request.Id] = request.NextIndex;
            LogsReplicated[request.NextIndex]++;

            if (LogsReplicated[request.NextIndex] == MajorityVote)
            {
                IncreaseCommitedLogs(LogsReplicated[request.NextIndex]);
            }
        }

        await Task.CompletedTask;
    }

    public void IncreaseFollowerCommitedLogs(int nextIndexToCommit)
    {
        StartElectionTimer();
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
                node.RequestVoteRPC(new(Term, Id));
            }
        }
    }

    public async Task RequestVoteRPC(RequestVoteData voteRequest)
    {
        if (Paused)
        {
            return;
        }
        if (Term > voteRequest.TermId)
        {
            return;
        }

        var candidateNode = OtherNodes.Where(x => x.Id == voteRequest.CandidateId).FirstOrDefault();

        if (candidateNode is not null)
        {
            if (!CurrentTermVotes.ContainsKey(voteRequest.TermId))
            {
                CurrentTermVotes[voteRequest.TermId] = voteRequest.CandidateId;
                candidateNode.CastVoteRPC(new(voteRequest.TermId, true));
            }
            candidateNode.CastVoteRPC(new(voteRequest.TermId, false));
        }
    }
    public async Task CastVoteRPC(CastVoteData voteRequest)
    {
        if (Paused)
        {
            return;
        }
        if (voteRequest.Vote && voteRequest.CandidateId == Term)
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
