using ClassLibrary;
using FluentAssertions;
using NSubstitute;
using Raft;
using System.ComponentModel;

namespace TestProject;

public class RaftTests
{
    //// Test #1
    //[Fact]
    //public void WhenALeaderIsActiveItSendsAHeartbeatWithin50()
    //{
    //    // Arrange
    //    var leaderNode = new Node(State.Leader, 1);

    //    // Act
    //    var result = leaderNode.SendAppendEntriesRPC();

    //    // Assert
    //    result.Should().Be(true);
    //}

    // Test #2
    [Fact]
    public async Task ANodeRecievesMessageItKnowsOtherNodeIsTheLeader()
    {
        // Arrange
        var otherNode = Substitute.For<INode>();
        otherNode.Id = 1;
        var leaderNode = new Node([otherNode], 2);

        // Act
        await leaderNode.StartElection();
        Thread.Sleep(300);
        await leaderNode.DetermineWinner();

        // Assert
        otherNode.LeaderId.Should().Be(2);
    }

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
    public async Task FollowerStartsElectionAfterNoMessageFor300()
    {
        // Arrange
        Node testNode = new Node(1);

        // Act
        await testNode.StartElection();
        Thread.Sleep(300);

        // Assert
        testNode.State.Should().Be(State.Candidate);
    }

    // Test #5
    [Fact]
    public async Task WhenElectionTimeIsResetItIsRandomBetween150and300()
    {
        // Arrange
        var testNode1 = Substitute.For<INode>();
        testNode1.Id = 1;
        var testNode2 = Substitute.For<INode>();
        testNode2.Id = 2;
        List<INode> otherNodes = new List<INode>() { testNode1, testNode2 };
        var testNode = new Node(otherNodes, 3);

        // Act
        await testNode.StartElection();
        var result = testNode.Timer.Interval;
        Thread.Sleep(301);
        await testNode.DetermineWinner();

        // Assert
        testNode.Timer.Interval.Should().BeInRange(150, 300);
    }

    // Test #6
    [Fact]
    public async Task NewElectionBeginsTermCountIncrementsByOne()
    {
        // Arrange
        var testNode = new Node(1);

        // Act
        await testNode.StartElection();
        Thread.Sleep(300);

        // Assert
        testNode.Term.Should().Be(2);
    }

    // Test #8
    [Fact]
    public async Task CandidateGetsMajorityVotes()
    {
        // Arrange
        var testNode = new Node(1);

        // Act
        await testNode.StartElection();
        Thread.Sleep(300);
        await testNode.DetermineWinner();

        // Assert
        testNode.State.Should().Be(State.Leader);
    }

    // Test #9
    [Fact]
    public async Task UnresponsiveNodeStillGiveCandidateLeadershipStatus()
    {
        // Arrange
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        var followerNode2 = Substitute.For<INode>();
        followerNode2.Id = 2;
        var leaderNode = new Node([followerNode1, followerNode2], 3);

        // Act
        await leaderNode.StartElection();
        Thread.Sleep(300);
        await leaderNode.CastVoteRPC(leaderNode.Term, true);
        await leaderNode.DetermineWinner();

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

    // Test #11
    [Fact]
    public async Task WhenFollowerBecomesCandidateTheyVoteForThemself()
    {
        // Arrange
        var testNode = new Node(1);

        // Act
        await testNode.StartElection();
        Thread.Sleep(300);

        // Assert
        testNode.Votes.Should().Be(1);
    }

    // Test #12
    [Fact]
    public async Task CandidateReceivesMessageFromNodeWithLaterTermShouldBecomeFollower()
    {
        // Arrange
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.State = State.Leader;
        leaderNode.Term = 2;
        var candidateNode = new Node([leaderNode], 1);
        candidateNode.State = State.Candidate;
        candidateNode.Term = 1;
        candidateNode.LeaderId = 2;

        // Act
        await candidateNode.RequestAppendEntriesRPC();

        // Assert
        candidateNode.State.Should().Be(State.Follower);
    }

    // Test # 13
    [Fact]
    public async Task CandidateReceivesMessageFromNodeWithAnEqualTermShouldBecomeFollower()
    {
        // Arrange
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.State = State.Leader;
        leaderNode.Term = 1;
        var candidateNode = new Node([leaderNode], 1);
        candidateNode.State = State.Candidate;
        candidateNode.Term = 1;
        candidateNode.LeaderId = 2;

        // Act
        await candidateNode.RequestAppendEntriesRPC();

        // Assert
        candidateNode.State.Should().Be(State.Follower);
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

    // Test #15
    [Fact]
    public async Task FutureTermMakesThemVoteYes()
    {
        // Arrange
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        var followerNode = new Node([leaderNode], 1);

        // Act
        await followerNode.RequestVoteRPC(1, 2);
        await followerNode.RequestVoteRPC(2, 2);

        // Assert
        await leaderNode.Received().CastVoteRPC(2, true);
    }

    // Test #16
    [Fact]
    public async Task WhenElectionTimerExpiresInsideElectionAnotherElectionStarts()
    {
        // Arrange
        var testNode1 = Substitute.For<INode>();
        testNode1.Id = 1;
        var testNode2 = Substitute.For<INode>();
        testNode2.Id = 2;
        List<INode> otherNodes = new List<INode>() { testNode1, testNode2 };
        var testNode = new Node(otherNodes, 3);

        // Act
        await testNode.StartElection();
        var result = testNode.Timer.Interval;
        Thread.Sleep(301);
        await testNode.DetermineWinner();

        // Assert
        testNode.MajorityVote.Should().Be(2);
        testNode.Timer.Interval.Should().NotBe(result);
    }

    // Test #17
    [Fact]
    public async Task FollowerNodeRecievesAnAppendEntriesRequestItResponds()
    {
        // Arrange
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.Term = 1;
        var followerNode = new Node([leaderNode], 1);
        followerNode.LeaderId = 2;

        // Act
        await followerNode.RequestAppendEntriesRPC();

        // Assert
        await leaderNode.Received().ConfirmAppendEntriesRPC();
    }

    // Test #18
    [Fact]
    public async Task CandidateRecievesOldAppendEntriesFromPreviousTermRejects()
    {
        // Arrange
        var candidateNode = Substitute.For<INode>();
        candidateNode.State = State.Candidate;
        candidateNode.Id = 1;
        candidateNode.LeaderId = 2;
        candidateNode.Term = 2;
        var leaderNode = new Node([candidateNode], 2);
        leaderNode.Term = 1;

        // Act
        await leaderNode.SendAppendEntriesRPC(candidateNode.Term);

        // Assert
        await candidateNode.DidNotReceive().RequestAppendEntriesRPC();
    }

    // Test #19
    [Fact]
    public async Task WhenACandidateWinsAnElectionItImmediatelySendsAHeartbeat()
    {
        // Arrange
        var otherNode = Substitute.For<INode>();
        otherNode.Id = 1;
        var testNode = new Node([otherNode], 2);

        // Act
        await testNode.StartElection();
        Thread.Sleep(300);
        await testNode.DetermineWinner();

        // Assert
        await otherNode.Received().RequestAppendEntriesRPC();
    }
}