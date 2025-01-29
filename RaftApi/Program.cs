using ClassLibrary;
using Raft;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

List<INode> otherNodes = [];

var node = new Node(otherNodes)
{
    Id = 1,
};

app.MapPost("/request/appendEntries", async () =>
{
    //await node.RequestAppendEntriesRPC();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();


