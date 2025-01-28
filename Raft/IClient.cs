namespace ClassLibrary;

public interface IClient
{
    public Task SendNodeMessage();
    public Task ReceiveNodeMessage();
}
