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
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        followerNode1.LeaderId = 2;
        var leaderNode = new Node([followerNode1], 2);

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
    public void NodeBecomesLeaderSetsNextIndexForEachFollowerToOneAfterLastIndex()
    {
        // Arrange
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        followerNode1.LeaderId = 2;
        var leaderNode = new Node([followerNode1], 2);
        leaderNode.FollowersNextIndex[followerNode1.Id] = 0;

        // Act
        leaderNode.BecomeLeader();
        leaderNode.NextIndex.Should().Be(0);
        leaderNode.RecieveClientCommand("test");
        leaderNode.RecieveClientCommand("test 2");
        leaderNode.SendAppendEntriesRPC(leaderNode.Term, leaderNode.NextIndex);

        // Assert
        leaderNode.NextIndex.Should().Be(2);
        followerNode1.NextIndex.Should().Be(2); 
    }

    // Test #5
    [Fact]
    public void LeaderMaintainsNextIndexForAllFollowers()
    {
        // Arrange
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        followerNode1.LeaderId = 2;
        var followerNode2 = Substitute.For<INode>();
        followerNode2.Id = 3;
        followerNode2.LeaderId = 2;
        var leaderNode = new Node([followerNode2, followerNode1], 2);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("test");
        leaderNode.RecieveClientCommand("test 2");

        leaderNode.FollowersNextIndex[followerNode2.Id] = 2;
        leaderNode.FollowersNextIndex[followerNode1.Id] = 1;

        leaderNode.SendAppendEntriesRPC(leaderNode.Term, leaderNode.NextIndex);

        // Assert
        followerNode2.Received().RequestAppendEntriesRPC(1, 2, 0, 0, Arg.Is<List<Log>>(logs =>
            logs.Count == 1 &&
            logs[0].Command == "test 2"), 0);

        followerNode1.Received().RequestAppendEntriesRPC(1, 2, 0, 0, Arg.Is<List<Log>>(logs =>
            logs.Count == 2 &&
            logs[0].Command == "test" &&
            logs[1].Command == "test 2"), 0);
    }

    // Test #6
    [Fact]
    public void HighestCommitedIndexFromLeaderIsIncludedInAppendEntries()
    {
        // Arrange
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        followerNode1.LeaderId = 2;
        var leaderNode = new Node([followerNode1], 2);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("test");
        leaderNode.SendAppendEntriesRPC(leaderNode.Term, leaderNode.NextIndex);

        // Assert
        followerNode1.Received().RequestAppendEntriesRPC(1, 2, 0, 0, Arg.Any<List<Log>>(), 1);
    }

    // Test #7
    [Fact]
    public async Task FollowerLearnsThatLeaderCommitedItCommitsToItsLocalStateMachine()
    {
        // Arrange
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.Term = 1;
        leaderNode.StateMachine = new Dictionary<int, string>() { { 1, "test" } };
        var followerNode = new Node([leaderNode], 2);
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
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        followerNode1.LeaderId = 2;
        var leaderNode = new Node([followerNode1], 2);

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
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        followerNode1.LeaderId = 2;
        var leaderNode = new Node([followerNode1], 2);

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
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.Term = 1;
        leaderNode.StateMachine = new();
        var followerNode1 = new Node([leaderNode], 1);
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
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.Term = 1;
        leaderNode.StateMachine = new();
        var followerNode1 = new Node([leaderNode], 1);
        var logs = new List<Log>();
        var log = new Log(1, "test");
        logs.Add(log);

        // Act
        await followerNode1.RequestAppendEntriesRPC(1, 2, 0, 0, logs, 1);

        // Assert
        await leaderNode.Received().ConfirmAppendEntriesRPC(followerNode1.Term, followerNode1.NextIndex);
    }

    // Test #12
    //[Fact]
    //public async Task LeaderReceivesMajorityResponsesNextHeartbeatLeader()
    //{
    //    // Arrange
    //    var followerNode1 = Substitute.For<INode>();
    //    followerNode1.Id = 1;
    //    followerNode1.LeaderId = 2;
    //    var leaderNode = new Node([followerNode1], 2);

    //    // Act
    //    await leaderNode.ConfirmAppendEntriesRPC(1, 1);

    //    // Assert

    //}

    // Test #13
    [Fact]
    public void LeaderWhenCommitsLogsToStateMachine()
    {
        // Arrange
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        followerNode1.LeaderId = 2;
        var leaderNode = new Node([followerNode1], 2);

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
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.Term = 1;
        leaderNode.StateMachine = new Dictionary<int, string>() { { 1, "test" } };
        var followerNode = new Node([leaderNode], 2);
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
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.Term = 1;
        leaderNode.StateMachine = new Dictionary<int, string>() { { 1, "test" }, { 2, "test 2" } };
        var followerNode = new Node([leaderNode], 2);

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
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        followerNode1.LeaderId = 2;
        followerNode1.NextIndex = 0;

        var leaderNode = new Node([followerNode1], 2);
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
        await leaderNode.ConfirmAppendEntriesRPC(followerNode1.Term, followerNode1.NextIndex);

        // Assert
        leaderNode.NextIndex.Should().Be(1);
        Thread.Sleep(80);
        await followerNode1.Received().RequestAppendEntriesRPC(1, 2, 0, 0, Arg.Any<List<Log>>(), 1);
    }
}
