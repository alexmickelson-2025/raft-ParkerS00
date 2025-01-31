namespace ClassLibrary;

public record ConfirmAppendEntriesData
{
    public ConfirmAppendEntriesData(int term, int nextIndex, bool status, int id)
    {
        Term = term;
        NextIndex = nextIndex;
        Status = status;
        Id = id;
    }
    public int Term { get; set; }
    public int NextIndex { get; set; }
    public bool Status { get; set; }
    public int Id { get; set; }
}
