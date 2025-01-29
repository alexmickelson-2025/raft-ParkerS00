using ClassLibrary;
using FluentAssertions;
using NSubstitute;
using Raft;

namespace TestProject;

public class LoggingTests
{
    // Test #1
    [Fact]
    public void LeaderSendsLogToAllOtherNodesAfterClientCommand()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        var leaderNode = new Node([followerNode1], 2, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("test");

        // Assert
        followerNode1.Received().RequestAppendEntriesRPC(leaderNode.Term, 2, 0, 0, Arg.Is<List<Log>>(logs => 
            logs.Count == 1 &&
            logs[0].Term == 1 &&
            logs[0].Command == "test"), 0);
    }

    // Test #2
    [Fact]
    public void LeaderAppendsMessageToItsLogs()
    {
        // Arrange
        var leaderNode = new Node(1);

        // Act
        leaderNode.RecieveClientCommand("test command");

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
    public async Task NodeBecomesLeaderSetsNextIndexForEachFollowerToOneAfterLastIndex()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;

        var followerNode = new Node([leaderNode], 1, client);

        var logs = new List<Log>();
        var log = new Log(1, "test");
        logs.Add(log);
        var log2 = new Log(2, "test");
        logs.Add(log2);

        // Act
        await followerNode.RequestAppendEntriesRPC(1, 2, 1, 1, logs, 2);

        // Assert
        followerNode.NextIndex.Should().Be(2);
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
        leaderNode.RecieveClientCommand("test");
        leaderNode.RecieveClientCommand("test 2");
        leaderNode.BecomeLeader();

        Thread.Sleep(50);

        // Assert
        followerNode2.Received().RequestAppendEntriesRPC(1, 2, 1, 1, Arg.Is<List<Log>>(logs =>
            logs.Count == 1 &&
            logs[0].Command == "test 2"), 2);

        followerNode1.Received().RequestAppendEntriesRPC(1, 2, 0, 1, Arg.Is<List<Log>>(logs =>
            logs.Count == 2 &&
            logs[0].Command == "test" &&
            logs[1].Command == "test 2"), 2);
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
        leaderNode.RecieveClientCommand("test");
        leaderNode.SendAppendEntriesRPC(leaderNode.Term, leaderNode.NextIndex);

        // Assert
        followerNode1.Received().RequestAppendEntriesRPC(1, 2, 0, 1, Arg.Any<List<Log>>(), 1);
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
        var log = new Log(1, "test");
        logs.Add(log);

        // Act
        await followerNode.RequestAppendEntriesRPC(1, 2, 0, 1, logs, 1);

        // Assert
        followerNode.StateMachine.Count.Should().Be(1);
    }

    // Test #8
    [Fact]
    public void WhenLeaderHasRecievedMajorityConfirmationItCommitsLog()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;

        var leaderNode = new Node([followerNode1], 2, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("test");
        leaderNode.SendAppendEntriesRPC(leaderNode.Term, leaderNode.NextIndex);

        // Assert
        leaderNode.StateMachine.Count.Should().Be(1);
    }

    // Test #9
    [Fact]
    public void LeaderCommitsLogsByIncrementingItsCommitedLogIndex()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        var leaderNode = new Node([followerNode1], 2, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("test");
        leaderNode.SendAppendEntriesRPC(leaderNode.Term, leaderNode.NextIndex);

        // Assert
        leaderNode.LeaderCommitIndex.Should().Be(1);    
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
        var log = new Log(1, "test");
        logs.Add(log);  

        // Act
        await followerNode1.RequestAppendEntriesRPC(1, 2, 0, 0, logs, 1);

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
        var log = new Log(1, "test");
        logs.Add(log);

        // Act
        await followerNode1.RequestAppendEntriesRPC(1, 2, 0, 0, logs, 1);

        // Assert
        await leaderNode.Received().ConfirmAppendEntriesRPC(followerNode1.Term, followerNode1.NextIndex);
    }

    // Test #12
    [Fact]
    public async Task LeaderReceivesMajorityResponsesLeaderSendConfirmation()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var followerNode = Substitute.For<INode>();
        var leaderNode = new Node([followerNode], 2, client);
        leaderNode.RecieveClientCommand("test");

        // Act
        await leaderNode.ConfirmAppendEntriesRPC(1, 1);

        // Assert
        await client.Received().ReceiveNodeMessage();
    }

    // Test #13
    [Fact]
    public void LeaderWhenCommitsLogsToStateMachine()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;

        var leaderNode = new Node([followerNode1], 2, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("test");
        leaderNode.SendAppendEntriesRPC(leaderNode.Term, leaderNode.NextIndex);

        // Assert
        leaderNode.StateMachine.Count.Should().Be(1);
    }

    // Test #14
    [Fact]
    public async Task AfterLeaderNodeCommittsFollowerNodesCommitOnNextHeartbeat()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;

        var followerNode = new Node([leaderNode], 2, client);
        var logs = new List<Log>();
        var log = new Log(1, "test");
        logs.Add(log);

        // Act
        await followerNode.RequestAppendEntriesRPC(1, 2, 0, 1, logs, 1);

        // Assert
        followerNode.StateMachine.Count.Should().Be(1); 
    }

    // Test #15.1
    [Fact]
    public async Task FollowerDoesNotFindEntryInItsLogWithSameIndexAndTerm()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;

        var followerNode = new Node([leaderNode], 2, client);

        // Act
        await followerNode.RequestAppendEntriesRPC(1, 2, 0, 0, new List<Log>(), 1);

        // Assert
        followerNode.logs.Count.Should().Be(0);
        followerNode.StateMachine.Count.Should().Be(0);
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
        leaderNode.NextIndex = 2;
        leaderNode.Term = 1;
        leaderNode.State = State.Leader;

        var logs = new List<Log>();
        var log = new Log(1, "test");
        logs.Add(log);
        var log2 = new Log(2, "test");
        logs.Add(log2);
        leaderNode.logs = logs;

        // Act
        leaderNode.StartHeartbeatTimer();
        await leaderNode.ConfirmAppendEntriesRPC(1, 1);

        // Assert
        leaderNode.NextIndex.Should().Be(1);
        Thread.Sleep(100);
        await followerNode1.Received().RequestAppendEntriesRPC(1, 2, 0, 1, Arg.Any<List<Log>>(), 1);
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
        leaderNode.RecieveClientCommand("test");
        Thread.Sleep(60);

        // Assert
        leaderNode.StateMachine.Count.Should().Be(0);
        leaderNode.LeaderCommitIndex.Should().Be(0);
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
        followerNode1.Received().RequestAppendEntriesRPC(1, 2, 0, 0, Arg.Any<List<Log>>(), 0);
        Thread.Sleep(60);
        followerNode1.Received().RequestAppendEntriesRPC(1, 2, 0, 0, Arg.Any<List<Log>>(), 0);
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
        await followerNode.RequestAppendEntriesRPC(1, 2, 2, 1, new List<Log>(), 1);

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
        leaderNode.Term = 2;
        leaderNode.RecieveClientCommand("test");
        leaderNode.RecieveClientCommand("test 2");

        // Act
        leaderNode.BecomeLeader();

        // Assert
        await followerNode.Received().RequestAppendEntriesRPC(2, 2, 0, 2, Arg.Any<List<Log>>(), 2);
    }
}
