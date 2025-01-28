namespace ClassLibrary;

public class Client : IClient
{
    public Task ReceiveNodeMessage()
    {
        return Task.CompletedTask;
    }

    public Task SendNodeMessage()
    {
        return Task.CompletedTask;
    }
}
