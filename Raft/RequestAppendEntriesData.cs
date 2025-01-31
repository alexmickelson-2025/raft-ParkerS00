using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary;

public class RequestAppendEntriesData
{
    public RequestAppendEntriesData(int term, int leaderId, int prevLogIndex, int prevLogTerm, List<Log> entries, int leaderCommit)
    {
        Term = term;
        LeaderCommit = leaderCommit;
        LeaderId = leaderId;
        Entries = entries;
        PrevLogIndex = prevLogIndex;
        PrevLogTerm = prevLogTerm;
    }

    public int Term { get; set; }
    public int LeaderId { get; set; }
    public int PrevLogIndex { get; set; }
    public int PrevLogTerm { get; set; }
    public List<Log>? Entries { get; set; }
    public int LeaderCommit { get; set; }
}
