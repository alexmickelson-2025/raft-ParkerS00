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
    public Task CastVoteRPC(CastVoteData voteRequest)
    {
        if (Paused == true)
        {
            return Task.CompletedTask;
        }
        return ((INode)InnerNode).CastVoteRPC(voteRequest);
    }

    public Task ConfirmAppendEntriesRPC(ConfirmAppendEntriesData request)
    {
        if (Paused == true)
        {
            return Task.CompletedTask;
        }
        return ((INode)InnerNode).ConfirmAppendEntriesRPC(request);
    }

    public void Pause()
    {
        ((INode)InnerNode).Pause();
    }

    public void RecieveClientCommand(string key, string value)
    {
        ((INode)InnerNode).RecieveClientCommand(key, value);
    }

    public async Task RequestAppendEntriesRPC(RequestAppendEntriesData request)
    {
        if (Paused == true)
        {
            return;
        }
        await Task.Delay(NetworkDelay);
        await ((INode)InnerNode).RequestAppendEntriesRPC(request);
    }

    public async Task RequestVoteRPC(RequestVoteData voteRequest)
    {
        if (Paused == true)
        {
            return;
        }
        await Task.Delay(NetworkDelay);
        await ((INode)InnerNode).RequestVoteRPC(voteRequest);
    }

    public void UnPause()
    {
        ((INode)InnerNode).UnPause();
    }

    public void SendAppendEntriesRPC()
    {
        if (Paused == true)
        {
            return;
        }
        ((INode)InnerNode).SendAppendEntriesRPC();
    }
}
