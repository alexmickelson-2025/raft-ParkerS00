using ClassLibrary;
using Raft;
using RaftApi;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");

var app = builder.Build();

var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");
var nodeIntervalScalarRaw = Environment.GetEnvironmentVariable("NODE_INTERVAL_SCALAR") ?? throw new Exception("NODE_INTERVAL_SCALAR environment variable not set");

INode[] otherNodes = otherNodesRaw
  .Split(";")
  .Select(s => new HttpRpcOtherNode(int.Parse(s)))
  .ToArray();

var node = new Node([.. otherNodes])
{
    Id = int.Parse(nodeId),
};

app.MapPost("/request/appendEntries", async (RequestAppendEntriesData request) =>
{
    Console.WriteLine($"Received append entries request {request}");
    await node.RequestAppendEntriesRPC(request);
});

app.MapPost("/request/vote", async (RequestVoteData request) =>
{
    Console.WriteLine($"Received vote request {request}");
    await node.RequestVoteRPC(request);
});

app.MapPost("/response/appendEntries", async (ConfirmAppendEntriesData response) =>
{
    Console.WriteLine($"Received append entries response {response}");
    await node.ConfirmAppendEntriesRPC(response);
});

app.MapPost("/response/vote", async (CastVoteData response) =>
{
    Console.WriteLine($"Recieved vote response {response}");
    await node.CastVoteRPC(response);
});

app.Run();


