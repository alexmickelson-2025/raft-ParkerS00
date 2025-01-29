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
    public int NetworkDelay { get; set; }
    public bool Paused { get; set;  }
    public Task CastVoteRPC(int candidateId, bool vote)
    {
        if (Paused == true)
        {
            return Task.CompletedTask;
        }
        return ((INode)InnerNode).CastVoteRPC(candidateId, vote);
    }

    public Task ConfirmAppendEntriesRPC(int term, int nextIndex)
    {
        if (Paused == true)
        {
            return Task.CompletedTask;
        }
        return ((INode)InnerNode).ConfirmAppendEntriesRPC(term, nextIndex);
    }

    public void Pause()
    {
        ((INode)InnerNode).Pause();
    }

    public void RecieveClientCommand(string command)
    {
        ((INode)InnerNode).RecieveClientCommand(command);
    }

    public async Task RequestAppendEntriesRPC(int term, int leaderId, int prevLogIndex, int prevLogTerm, List<Log> entries, int leaderCommit)
    {
        if (Paused == true)
        {
            return;
        }
        await Task.Delay(NetworkDelay);
        await ((INode)InnerNode).RequestAppendEntriesRPC(term, leaderId, prevLogIndex, prevLogTerm, entries, leaderCommit);
    }

    public async Task RequestVoteRPC(int termId, int candidateId)
    {
        if (Paused == true)
        {
            return;
        }
        await Task.Delay(NetworkDelay);
        await ((INode)InnerNode).RequestVoteRPC(termId, candidateId);
    }

    public void UnPause()
    {
        ((INode)InnerNode).UnPause();
    }
}
