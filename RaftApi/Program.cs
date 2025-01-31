using ClassLibrary;
using Raft;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

List<INode> otherNodes = [];

var node = new Node(otherNodes)
{
    Id = 1,
};

app.MapPost("/request/appendEntries", async () =>
{
    Console.WriteLine("Sending append entries");
    //await node.RequestAppendEntriesRPC();
});

app.UseHttpsRedirection();

app.Run();


