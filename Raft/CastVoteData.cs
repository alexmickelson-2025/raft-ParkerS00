using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary;

public record CastVoteData
{
    public CastVoteData(int candidateId, bool vote)
    {
        CandidateId = candidateId;
        Vote = vote;
    }

    public int CandidateId { get; set; }
    public bool Vote { get; set; }
}
