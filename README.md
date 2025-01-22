# raft-ParkerS00
[X] When a leader is active it sends a heart beat within 50ms.

[X] When a node receives an AppendEntries from another node, then first node remembers that other node is the current leader.

[X] When a new node is initialized, it should be in follower state.

[X] When a follower doesn't get a message for 300ms then it starts an election.

[X] When the election time is reset, it is a random value between 150 and 300ms.

[X] When a new election begins, the term is incremented by 1.

[X] When a follower does get an AppendEntries message, it resets the election timer. 

[X] Given an election begins, when the candidate gets a majority of votes, it becomes a leader. 

[X] Given a candidate receives a majority of votes while waiting for unresponsive node, it still becomes a leader.
A follower that has not voted and is in an earlier term responds to a RequestForVoteRPC with yes. 

[X] Given a candidate server that just became a candidate, it votes for itself.

[X] Given a candidate, when it receives an AppendEntries message from a node with a later term, then candidate loses and becomes a follower.

[X] Given a candidate, when it receives an AppendEntries message from a node with an equal term, then candidate loses and becomes a follower.

[X] If a node receives a second request for vote for the same term, it should respond no. 

[X] If a node receives a second request for vote for a future term, it should vote for that node.

[X] Given a candidate, when an election timer expires inside of an election, a new election is started.

[X] When a follower node receives an AppendEntries request, it sends a response.

[X] Given a candidate receives an AppendEntries from a previous term, then rejects.

[X] When a candidate wins an election, it immediately sends a heart beat.

[ ] when a leader receives a client command the leader sends the log entry in the next appendentries RPC to all nodes

[ ] when a leader receives a command from the client, it is appended to its log

[ ] when a node is new, its log is empty

[ ] when a leader wins an election, it initializes the nextIndex for each follower to the index just after the last one it its log

[ ] leaders maintain an "nextIndex" for each follower that is the index of the next log entry the leader will send to that follower

[ ] Highest committed index from the leader is included in AppendEntries RPC's

[ ] When a follower learns that a log entry is committed, it applies the entry to its local state machine

[ ] when the leader has received a majority confirmation of a log, it commits it

[ ] the leader commits logs by incrementing its committed log index

[ ] given a follower receives an appendentries with log(s) it will add those entries to its personal log

[ ] a followers response to an appendentries includes the followers term number and log entry index

[ ] when a leader receives a majority responses from the clients after a log replication heartbeat, the leader sends a confirmation response to the client

[ ] given a leader node, when a log is committed, it applies it to its internal state machine

[ ] when a follower receives a heartbeat, it increases its commitIndex to match the commit index of the heartbeat

[ ] When sending an AppendEntries RPC, the leader includes the index and term of the entry in its log that immediately precedes the new entries

[ ] when a leader sends a heartbeat with a log, but does not receive responses from a majority of nodes, the entry is uncommitted

[ ] if a leader does not response from a follower, the leader continues to send the log entries in subsequent heartbeats

[ ] if a leader cannot commit an entry, it does not send a response to the client

[ ] if a node receives an appendentries with a logs that are too far in the future from your local state, you should reject the appendentries

[ ] if a node receives and appendentries with a term and index that do not match, you will reject the appendentry until you find a matching log 
