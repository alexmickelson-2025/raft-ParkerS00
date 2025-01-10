namespace Raft;

public class Node
{
    public Node()
    {
        IsFollower = true;
    }

    public bool IsFollower { get; set; }
}
