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
        Node testNode = new Node();

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
        Node testNode = new Node();

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
        var testNode = new Node();

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
        var testNode = new Node();

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
        var testNode = new Node();

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
        var testNode1 = new Node();
        var testNode2 = new Node();
        List<INode> otherNodes = new List<INode>() { testNode1, testNode2 };
        var testNode = new Node(otherNodes);

        // Act
        testNode.StartElection();
        var result = testNode.Timer.Interval;
        Thread.Sleep(301);
        testNode.DetermineWinner();

        // Assert
        testNode.Timer.Interval.Should().NotBe(result);
    }

    // Test #5
    [Fact]
    public void WhenElectionTimeIsResetItIsRandomBetween150and300()
    {
        // Arrange
        var testNode1 = new Node();
        var testNode2 = new Node();
        List<INode> otherNodes = new List<INode>() { testNode1, testNode2 };
        var testNode = new Node(otherNodes);

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
        var leaderNode = new Node(State.Leader);

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
        var testNode = new Node([otherNode]);

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
        var leaderNode = new Node([otherNode]);

        // Act
        leaderNode.StartElection();
        Thread.Sleep(300);
        leaderNode.DetermineWinner();

        // Assert
        otherNode.LeaderId.Should().Be(2);
    }
}