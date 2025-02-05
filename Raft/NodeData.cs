namespace ClassLibrary;

public record NodeData
{
    public NodeData(int id,
                    int leaderId,
                    int term,
                    int commitIndex,
                    List<Log> logs,
                    State state,
                    Dictionary<string, string> stateMachine,
                    System.Timers.Timer timer,
                    DateTime startTime)
    {
        Id = id;
        LeaderId = LeaderId;
        Term = term;
        CommitIndex = commitIndex;
        Logs = logs;
        State = state;
        StateMachine = stateMachine;
        Timer = timer;
        StartTime = startTime;
    }

    public int Id { get; set; } 
    public int LeaderId { get; set; }
    public int Term { get; set; }
    public int CommitIndex { get; set; }
    public List<Log>? Logs { get; set; }
    public State State { get; set; }
    public Dictionary<string, string> StateMachine { get; set; }
    public System.Timers.Timer Timer { get; set; }
    public DateTime StartTime { get; set; }
}
