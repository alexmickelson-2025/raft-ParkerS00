using ClassLibrary;

namespace RaftApi;

public class HttpRpcOtherNode : INode
{
    public int Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool Paused { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Task CastVoteRPC(int candidateId, bool vote)
    {
        throw new NotImplementedException();
    }

    public Task ConfirmAppendEntriesRPC(int term, int nextIndex, bool status, int id)
    {
        throw new NotImplementedException();
    }

    public void Pause()
    {
        throw new NotImplementedException();
    }

    public void RecieveClientCommand(string key, string value)
    {
        throw new NotImplementedException();
    }

    public Task RequestAppendEntriesRPC(int term, int leaderId, int prevLogIndex, int prevLogTerm, List<Log> entries, int leaderCommit)
    {
        throw new NotImplementedException();
    }

    public Task RequestVoteRPC(int termId, int candidateId)
    {
        throw new NotImplementedException();
    }

    public void SendAppendEntriesRPC()
    {
        throw new NotImplementedException();
    }

    public void UnPause()
    {
        throw new NotImplementedException();
    }
}
