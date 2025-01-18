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
	public System.Timers.Timer Timer { get; set; } = new();
	public DateTime StartTime { get; set; }
	public State State { get; set; }
	public List<INode> OtherNodes { get; set; }
	public Dictionary<int, int> CurrentTermVotes { get; set; } = new();
	public int MajorityVote { get => (OtherNodes.Count / 2) + 1; }
	public int MaxDelay { get; set; } = 300;
	public int MinDelay { get; set; } = 150;
	public int LeaderDelay { get; set; } = 50;

	public void StartElection()
	{
		State = State.Candidate;
		Term += 1;
		Votes = 1;
		SendVoteRequestRPC();
		StartElectionTimer();
	}

	public void StartElectionTimer()
	{
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

	public void ResetElectionTimer()
	{
		Timer.Stop();
		Timer.Dispose();
		Timer = new(Random.Shared.Next(MinDelay, MaxDelay));
		Timer.Elapsed += (s, e) =>
        {
            StartElection();
        };
        Timer.AutoReset = false;
        StartTime = DateTime.Now;
		Timer.Start();
	}

	public void BecomeLeader()
	{
		Timer.Stop();
		Timer.Dispose();
		State = State.Leader;
		LeaderId = Id;
		SendAppendEntriesRPC(Term);
		StartHeartbeatTimer();
	}

	public void StartHeartbeatTimer()
	{
        Timer.Stop();
        Timer.Dispose();
        Timer = new(LeaderDelay);
        StartTime = DateTime.Now;
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
		StartTime = DateTime.Now;
		if (Term >= termId)
		{
			foreach (var node in OtherNodes)
			{
				node.LeaderId = Id;
				node.State = State.Follower;
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
				ResetElectionTimer();
				Term = currentNode.Term;
				await currentNode.ConfirmAppendEntriesRPC();
				State = State.Follower;
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
			if (!CurrentTermVotes.ContainsKey(termId))
			{
				CurrentTermVotes[termId] = candidateId;
				await currentNode.CastVoteRPC(termId, true);
			}
			await currentNode.CastVoteRPC(termId, false);
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
}
