using ClassLibrary;

namespace RaftApi;

public class HttpRpcOtherNode : INode
{
    public int Id { get; set; }
    public bool Paused { get; set; }
    private string Url { get => $"http://node{Id}:8080"; }
    private HttpClient httpClient = new();

    public HttpRpcOtherNode(int id)
    {
        Id = id;
    }

    public async Task CastVoteRPC(CastVoteData voteRequest)
    {
        try
        {
            await httpClient.PostAsJsonAsync($"{Url}/response/vote", voteRequest);
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"node {Url} is down");
        }
    }

    public async Task ConfirmAppendEntriesRPC(ConfirmAppendEntriesData request)
    {
        try
        {
            await httpClient.PostAsJsonAsync($"{Url}/response/vote", request);
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"node {Url} is down");
        }
    }

    public void Pause()
    {
        throw new NotImplementedException();
    }

    public void RecieveClientCommand(string key, string value)
    {
        throw new NotImplementedException();
    }

    public async Task RequestAppendEntriesRPC(RequestAppendEntriesData request)
    {
        try
        {
            await httpClient.PostAsJsonAsync($"{Url}/request/appendEntries", request);
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"node {Url} is down");
        }
    }

    public async Task RequestVoteRPC(RequestVoteData voteRequest)
    {
        try
        {
            await httpClient.PostAsJsonAsync($"{Url}/request/vote", voteRequest);
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"node {Url} is down");
        }
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
