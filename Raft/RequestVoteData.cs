namespace ClassLibrary;

public record RequestVoteData
{
    public RequestVoteData(int termId, int candidateId)
    {
        TermId = termId;
        CandidateId = candidateId;
    }
    public int TermId { get; set; }
    public int CandidateId { get; set; }
}
