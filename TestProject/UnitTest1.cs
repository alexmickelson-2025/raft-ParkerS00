using ClassLibrary;
using FluentAssertions;
using NSubstitute;
using Raft;

namespace TestProject;

public class RaftTests
{
    // Test #3
    [Fact]
    public void WhenNodeIsCreatedItIsAFollower()
    {
        // Arrange
        Node testNode = new Node(1);

        // Act 
        var result = testNode.State;

        // Assert
        testNode.State.Should().Be(result);
    }

    // Test #4
    [Fact]
    public void FollowerStartsElectionAfterNoMessageFor300()
    {
        // Arrange
        Node testNode = new Node(1);

        // Act
        testNode.StartElection();
        Thread.Sleep(300);

        // Assert
        testNode.State.Should().Be(State.Candidate);
    }

    // Test #8
    [Fact]
    public void CandidateGetsMajorityVotes()
    {
        // Arrange
        var testNode = new Node(1);

        // Act
        testNode.StartElection();
        Thread.Sleep(300);
        testNode.DetermineWinner();

        // Assert
        testNode.State.Should().Be(State.Leader);
    }

    // Test #6
    [Fact]
    public void NewElectionBeginsTermCountIncrementsByOne()
    {
        // Arrange
        var testNode = new Node(1);

        // Act
        testNode.StartElection();
        Thread.Sleep(300);

        // Assert
        testNode.Term.Should().Be(2);
    }

    // Test #11
    [Fact]
    public void WhenFollowerBecomesCandidateTheyVoteForThemself()
    {
        // Arrange
        var testNode = new Node(1);

        // Act
        testNode.StartElection();
        Thread.Sleep(300);

        // Assert
        testNode.Votes.Should().Be(1);
    }

    // Test #16
    [Fact]
    public void WhenElectionTimerExpiresInsideElectionAnotherElectionStarts()
    {
        // Arrange
        var testNode1 = Substitute.For<INode>();
        testNode1.Id = 1;
        var testNode2 = Substitute.For<INode>();
        testNode2.Id = 2;
        List<INode> otherNodes = new List<INode>() { testNode1, testNode2 };
        var testNode = new Node(otherNodes, 3);

        // Act
        testNode.StartElection();
        var result = testNode.Timer.Interval;
        Thread.Sleep(301);
        testNode.DetermineWinner();

        // Assert
        testNode.MajorityVote.Should().Be(2);
        testNode.Timer.Interval.Should().NotBe(result);
    }

    // Test #5
    [Fact]
    public void WhenElectionTimeIsResetItIsRandomBetween150and300()
    {
        // Arrange
        var testNode1 = Substitute.For<INode>();
        testNode1.Id = 1;
        var testNode2 = Substitute.For<INode>();
        testNode2.Id = 2;
        List<INode> otherNodes = new List<INode>() { testNode1, testNode2 };
        var testNode = new Node(otherNodes, 3);

        // Act
        testNode.StartElection();
        var result = testNode.Timer.Interval;
        Thread.Sleep(301);
        testNode.DetermineWinner();

        // Assert
        testNode.Timer.Interval.Should().BeInRange(150, 300);
    }

    // Test #1
    [Fact]
    public void WhenALeaderIsActiveItSendsAHeartbeatWithin50()
    {
        // Arrange
        var leaderNode = new Node(State.Leader, 1);

        // Act
        var result = leaderNode.SendAppendEntriesRPC();

        // Assert
        result.Result.Should().Be(true);
    }

    // Test #19
    [Fact]
    public void WhenACandidateWinsAnElectionItImmediatelySendsAHeartbeat()
    {
        // Arrange
        var otherNode = Substitute.For<INode>();
        otherNode.Id = 1;
        var testNode = new Node([otherNode], 2);

        // Act
        testNode.StartElection();
        Thread.Sleep(300);
        testNode.DetermineWinner();

        // Assert
        otherNode.Received().RecieveAppendEntriesRPC();

    }

    // Test #2
    [Fact]
    public void ANodeRecievesMessageItKnowsOtherNodeIsTheLeader()
    {
        // Arrange
        var otherNode = Substitute.For<INode>();
        otherNode.Id = 1;
        var leaderNode = new Node([otherNode], 2);

        // Act
        leaderNode.StartElection();
        Thread.Sleep(300);
        leaderNode.DetermineWinner();

        // Assert
        otherNode.LeaderId.Should().Be(2);
    }

    // Test #9
    [Fact]
    public void UnresponsiveNodeStillGiveCandidateLeadershipStatus()
    {
        // Arrange
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        var followerNode2 = Substitute.For<INode>();
        followerNode2.Id = 2;
        var leaderNode = new Node([followerNode1, followerNode2], 3);

        // Act
        leaderNode.StartElection();
        Thread.Sleep(300);
        leaderNode.CastVoteRPC(leaderNode.Term, true);
        leaderNode.DetermineWinner();

        // Assert
        leaderNode.State.Should().Be(State.Leader);
    }

    // Test #10
    [Fact]
    public async Task FollowerHasntVotedYetRespondsWithYesForRequestToVote()
    {
        // Arrange
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        var followerNode = new Node([leaderNode], 1);

        // Act
        await followerNode.RequestVoteRPC(1, 2);

        // Assert
        await leaderNode.Received().CastVoteRPC(2, true);
    }

    // Test #14
    [Fact]
    public async Task FollowerWontVoteTwiceForSameTerm()
    {
        // Arrange
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        var followerNode = new Node([leaderNode], 1);

        // Act
        await followerNode.RequestVoteRPC(1, 2);
        await followerNode.RequestVoteRPC(1, 2);

        // Assert
        await leaderNode.Received().CastVoteRPC(2, false);
    }
}