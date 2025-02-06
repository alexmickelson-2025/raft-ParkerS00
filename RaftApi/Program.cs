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
    //Console.WriteLine($"Received append entries request," +
    //    $"LeaderId: {request.LeaderId}, Leader Commit: {request.LeaderCommit}, Term: {request.Term}, Entries: {request.Entries.Count}");
    await node.RequestAppendEntriesRPC(request);
});

app.MapPost("/request/vote", async (RequestVoteData request) =>
{
    //Console.WriteLine($"Received vote request {request.CandidateId}");
    await node.RequestVoteRPC(request);
});

app.MapPost("/response/appendEntries", async (ConfirmAppendEntriesData response) =>
{
    //Console.WriteLine($"Received append entries response {response.NextIndex}");
    await node.ConfirmAppendEntriesRPC(response);
});

app.MapPost("/response/vote", async (CastVoteData response) =>
{
    //Console.WriteLine($"Recieved vote response {response}");
    await node.CastVoteRPC(response);
});

app.MapPost("/command", (ClientCommand command) =>
{
    Console.WriteLine($"Command send to leader node, Key: {command.Key}, Value: {command.Value}");
    node.RecieveClientCommand(command.Key, command.Value);
});

app.MapGet("/nodeData", () =>
{
    foreach (var value in node.StateMachine)
    {
        Console.WriteLine($"Returning NodeData: StateMachine: {value.Key}, {value.Value}");
    }
    return new NodeData(node.Id, node.LeaderId, node.Term, node.CommitIndex, node.logs, node.State, node.StateMachine, node.Timer, node.StartTime);
});


app.Run();


