namespace ClassLibrary;

public record NodeData
{
    public int Id { get; set; } 
    public int LeaderId { get; set; }
    public int Term { get; set; }
    public int CommitIndex { get; set; }
    public List<Log>? Logs { get; set; }
    public State State { get; set; }
}
