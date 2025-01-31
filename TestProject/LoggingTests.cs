using ClassLibrary;
using FluentAssertions;
using NSubstitute;
using Raft;

namespace TestProject;

public class LoggingTests
{
    // Test #1
    [Fact]
    public async Task LeaderSendsLogToAllOtherNodesAfterClientCommand()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;

        var leaderNode = new Node([followerNode1], 2, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("key", "value");

        // Assert
        await followerNode1.Received().RequestAppendEntriesRPC(Arg.Is<RequestAppendEntriesData>(dto => dto.Entries.Count == 1 &&
                                                                                                dto.Entries[0].Term == 0 &&
                                                                                                dto.Entries[0].Key == "key" &&
                                                                                                dto.Entries[0].Value == "value"));
    }

    // Test #2
    [Fact]
    public void LeaderAppendsMessageToItsLogs()
    {
        // Arrange
        var leaderNode = new Node(1);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("key", "value");

        // Assert
        leaderNode.logs.Count.Should().Be(1);
    }

    // Test #3
    [Fact]
    public void NewNodeHasEmptyLog()
    {
        // Arrange
        var node = new Node(1);

        // Act


        // Assert
        node.logs.Count.Should().Be(0);
    }

    // Test #4
    [Fact]
    public void NodeBecomesLeaderSetsNextIndexForEachFollowerToOneAfterLastIndex()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode = Substitute.For<INode>();
        followerNode.Id = 1;

        var leaderNode = new Node([followerNode], 2, client);

        // Act
        leaderNode.BecomeLeader();

        // Assert
        leaderNode.FollowersNextIndex[followerNode.Id].Should().Be(0);
    }

    // Test #5
    [Fact]
    public void LeaderMaintainsNextIndexForAllFollowers()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;

        var followerNode2 = Substitute.For<INode>();
        followerNode2.Id = 3;

        var leaderNode = new Node([followerNode2, followerNode1], 2, client);
        leaderNode.FollowersNextIndex[followerNode1.Id] = 1;
        leaderNode.FollowersNextIndex[followerNode2.Id] = 2;

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("key", "value");
        leaderNode.BecomeLeader();

        // Assert
        leaderNode.FollowersNextIndex[followerNode1.Id].Should().Be(1);
        leaderNode.FollowersNextIndex[followerNode2.Id].Should().Be(1);
    }

    // Test #6
    [Fact]
    public void HighestCommitedIndexFromLeaderIsIncludedInAppendEntries()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;

        var leaderNode = new Node([followerNode1], 2, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("key", "value");
        leaderNode.SendAppendEntriesRPC();

        // Assert
        followerNode1.Received().RequestAppendEntriesRPC(Arg.Is<RequestAppendEntriesData>(dto => dto.Term == 0 && 
                                                                                            dto.LeaderId == 2 && 
                                                                                            dto.PrevLogIndex == 0 &&
                                                                                            dto.PrevLogTerm == 0 && 
                                                                                            dto.LeaderCommit == 0));
        leaderNode.CommitIndex.Should().Be(0);
    }

    // Test #7
    [Fact]
    public async Task FollowerLearnsThatLeaderCommitedItCommitsToItsLocalStateMachine()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;

        var followerNode = new Node([leaderNode], 2, client);

        var logs = new List<Log>();
        var log = new Log(1, "key", "value");
        logs.Add(log);

        followerNode.logs = logs;

        // Act
        await followerNode.RequestAppendEntriesRPC(new RequestAppendEntriesData(1, 2, 0, 1, logs, 1));

        // Assert
        followerNode.StateMachine["key"].Should().Be("value");
    }

    // Test #8
    [Fact]
    public async Task WhenLeaderHasRecievedMajorityConfirmationItCommitsLog()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;

        var leaderNode = new Node([followerNode1], 2, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("key", "value");
        leaderNode.SendAppendEntriesRPC();
        await leaderNode.ConfirmAppendEntriesRPC(new ConfirmAppendEntriesData(0, 1, true, 1));

        // Assert
        leaderNode.StateMachine["key"].Should().Be("value");
    }

    // Test #9
    [Fact]
    public async Task LeaderCommitsLogsByIncrementingItsCommitedLogIndex()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;

        var leaderNode = new Node([followerNode1], 2, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("key", "value");
        leaderNode.SendAppendEntriesRPC();
        await leaderNode.ConfirmAppendEntriesRPC(new ConfirmAppendEntriesData(0, 1, true, 1));

        // Assert
        leaderNode.CommitIndex.Should().Be(1);    
    }

    // Test #10
    [Fact]
    public async Task FollowerRecievesAppendEntriesCommitLogsToPersonalLog()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;

        var followerNode1 = new Node([leaderNode], 1, client);
        var logs = new List<Log>();
        var log = new Log(1, "key", "value");
        logs.Add(log);  

        // Act
        await followerNode1.RequestAppendEntriesRPC(new RequestAppendEntriesData(1, 2, 0, 0, logs, 0));

        // Assert
        followerNode1.logs.Count.Should().Be(1);
    }

    // Test #11
    [Fact]
    public async Task FollowersResponseIncludesTheFollowersTermNumberAndLogEntryIndex()
    {
        // Arrange 
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;

        var followerNode1 = new Node([leaderNode], 1, client);

        var logs = new List<Log>();
        var log = new Log(1, "key", "value");
        logs.Add(log);


        // Act
        await followerNode1.RequestAppendEntriesRPC(new RequestAppendEntriesData(1, 2, 0, 0, logs, 0));

        // Assert
       await leaderNode.Received().ConfirmAppendEntriesRPC(new ConfirmAppendEntriesData(1, 1, true, 1));
    }

    // Test #12
    [Fact]
    public async Task LeaderReceivesMajorityResponsesLeaderSendConfirmation()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode = Substitute.For<INode>();
        followerNode.Id = 1;

        var leaderNode = new Node([followerNode], 2, client);
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("key", "value");

        // Act
        await leaderNode.ConfirmAppendEntriesRPC(new ConfirmAppendEntriesData(0, 1, true, 1));

        // Assert
        await client.Received().ReceiveNodeMessage();
    }

    // Test #13
    [Fact]
    public async Task WhenLeaderCommitesLogItIsAppliedToItsInternalStateMachine()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;

        var leaderNode = new Node([followerNode1], 2, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("key", "value");
        leaderNode.SendAppendEntriesRPC();
        await leaderNode.ConfirmAppendEntriesRPC(new ConfirmAppendEntriesData(0, 1, true, 1));

        // Assert
        leaderNode.StateMachine["key"].Should().Be("value");
    }

    // Test #14
    [Fact]
    public async Task AfterLeaderNodeCommitsFollowerNodesCommitOnNextHeartbeat()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;

        var followerNode = new Node([leaderNode], 2, client);
        var logs = new List<Log>();
        var log = new Log(1, "key", "value");
        logs.Add(log);
        followerNode.logs = logs;

        // Act
        await followerNode.RequestAppendEntriesRPC(new RequestAppendEntriesData(1, 2, 0, 0, logs, 1));

        // Assert
        followerNode.StateMachine["key"].Should().Be("value"); 
    }

    // Test #15.1
    [Fact]
    public async Task FollowerDoesNotFindEntryInItsLogWithSameIndexAndTermThenItRefuses()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;

        var logs = new List<Log>();
        var log = new Log(1, "key", "value");

        var followerNode = new Node([leaderNode], 2, client);
        followerNode.LeaderId = 2;
        followerNode.logs.Add(log);

        var log2 = new Log(2, "key", "value");
        var log3 = new Log(2, "key", "value");
        logs.Add(log2);
        logs.Add(log3);

        // Act
        await followerNode.RequestAppendEntriesRPC(new RequestAppendEntriesData(1, 2, 2, 2, logs, 0));

        // Assert
        await leaderNode.Received().ConfirmAppendEntriesRPC(new ConfirmAppendEntriesData(1, 1, false, 2));
    }

    // Test #15.2
    [Fact]
    public async Task IfFollowerRejectsLeaderDecrementsNextIndexAndTriesAgain()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;

        var leaderNode = new Node([followerNode1], 2, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("key", "value");
        leaderNode.RecieveClientCommand("key 2", "value 2");
        await leaderNode.ConfirmAppendEntriesRPC(new ConfirmAppendEntriesData(0, 0, false, 1));

        // Assert
        Thread.Sleep(50);
        await followerNode1.Received().RequestAppendEntriesRPC(Arg.Is<RequestAppendEntriesData>(dto => dto.Term == 0 &&
                                                                                                dto.LeaderId == 2 &&
                                                                                                dto.PrevLogIndex == 0 &&
                                                                                                dto.PrevLogTerm == 0 &&
                                                                                                dto.LeaderCommit == 0));
    }

    // Test #16
    [Fact]
    public void LeaderSendsHeartbeatWithLogButDoesntRecieveMajorityDoesntCommitLogs()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode1 = Substitute.For<INode>();

        var followerNode2 = Substitute.For<INode>();

        var leaderNode = new Node([followerNode1, followerNode2], 2, client);

        // Act
        leaderNode.RecieveClientCommand("key", "value");
        Thread.Sleep(60);

        // Assert
        leaderNode.StateMachine.Count.Should().Be(0);
        leaderNode.CommitIndex.Should().Be(0);
    }

    // Test #17
    [Fact]
    public void IfLeaderDoesntRecieveResponseFromFollowerLeaderContinuesToSendTheLogEntriesInHearbeats()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;

        var leaderNode = new Node([followerNode1], 2, client);
        leaderNode.BecomeLeader();

        // Act
        Thread.Sleep(60);

        // Assert
        followerNode1.Received().RequestAppendEntriesRPC(Arg.Is<RequestAppendEntriesData>(dto => dto.Term == 0 &&
                                                                                                dto.LeaderId == 2 &&
                                                                                                dto.PrevLogIndex == 0 &&
                                                                                                dto.PrevLogTerm == 0 &&
                                                                                                dto.LeaderCommit == 0));
        Thread.Sleep(60);
        followerNode1.Received().RequestAppendEntriesRPC(Arg.Is<RequestAppendEntriesData>(dto => dto.Term == 0 &&
                                                                                                dto.LeaderId == 2 &&
                                                                                                dto.PrevLogIndex == 0 &&
                                                                                                dto.PrevLogTerm == 0 &&
                                                                                                dto.LeaderCommit == 0));
    }

    // Test #18
    [Fact]
    public void LeaderCannotCommitDoesntSendConfirmationToClient()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var followerNode = Substitute.For<INode>();
        var leaderNode = new Node([followerNode], 2, client);

        // Act
        leaderNode.BecomeLeader();

        // Assert
        client.DidNotReceive().ReceiveNodeMessage();
    }

    // Test #19
    [Fact]
    public async Task NodeRecievesAppendEntriesTooFarInFutureShouldReject()
    {
        // Arrange
        var followerNode = new Node(1);

        // Act
        await followerNode.RequestAppendEntriesRPC(new RequestAppendEntriesData(1, 2, 2, 1, new List<Log>(), 1));

        // Assert
        followerNode.StateMachine.Count.Should().Be(0); 
    }

    // Test #20
    [Fact]
    public async Task NodeRecievesAppendEntriesWithNonMatchingTermAndIndexRejectUntilMatchingLogFound()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode = Substitute.For<INode>();
        followerNode.Id = 1;

        var leaderNode = new Node([followerNode], 2, client);
        
        leaderNode.BecomeLeader();

        // Act
        leaderNode.RecieveClientCommand("key", "value");
        leaderNode.RecieveClientCommand("key 1", "value 1");

        // Assert
        await followerNode.Received().RequestAppendEntriesRPC(Arg.Is<RequestAppendEntriesData>(dto => dto.Term == 0 &&
                                                                                                dto.LeaderId == 2 &&
                                                                                                dto.PrevLogIndex == 0 &&
                                                                                                dto.PrevLogTerm == 0 &&
                                                                                                dto.LeaderCommit == 0));
    }
}
