using ClassLibrary;
using FluentAssertions;
using NSubstitute;
using Raft;

namespace TestProject;

public class RaftTests
{
    // Test #1
    [Fact]
    public async Task WhenALeaderIsActiveItSendsAHeartbeatWithin50()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var followerNode = Substitute.For<INode>();
        followerNode.Id = 2;
        var leaderNode = new Node([followerNode], 1, client);
        leaderNode.logs = new List<Log>();
        leaderNode.BecomeLeader();
        
        // Act
        Thread.Sleep(420);

        // Assert
        await followerNode.Received(9).RequestAppendEntriesRPC(leaderNode.Term, leaderNode.Id, 0, 0, Arg.Any<List<Log>>(), 0);
    }

    // Test #2
    [Fact]
    public void ANodeRecievesMessageItKnowsOtherNodeIsTheLeader()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var otherNode = Substitute.For<INode>();
        otherNode.Id = 1;
        var leaderNode = new Node([otherNode], 2, client);

        // Act
        leaderNode.StartElection();
        Thread.Sleep(320);
        leaderNode.DetermineWinner();

        // Assert
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
    public void FollowerStartsElectionAfterNoMessageFor300()
    {
        // Arrange
        Node testNode = new Node(1);

        // Act
        testNode.StartElection();
        Thread.Sleep(320);

        // Assert
        testNode.State.Should().Be(State.Candidate);
    }

    // Test #5
    [Fact]
    public void WhenElectionTimeIsResetItIsRandomBetween150and300()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var testNode1 = Substitute.For<INode>();
        testNode1.Id = 1;
        var testNode2 = Substitute.For<INode>();
        testNode2.Id = 2;
        List<INode> otherNodes = new List<INode>() { testNode1, testNode2 };
        var testNode = new Node(otherNodes, 3, client);
        var initialInterval = testNode.Timer.Interval;
        var collisions = 0;

        // Act
        for (var i = 0; i < 10; i++)
        {
            testNode.StartElection();
            Thread.Sleep(320);
            testNode.DetermineWinner();
            var result = testNode.Timer.Interval;

            if (initialInterval == result)
            {
                collisions++;
            }
        }

        // Assert
        collisions.Should().BeLessThan(5);
    }

    // Test #6
    [Fact]
    public void NewElectionBeginsTermCountIncrementsByOne()
    {
        // Arrange
        var testNode = new Node(1);

        // Act
        testNode.StartElection();

        // Assert
        testNode.Term.Should().Be(2);
    }

    // Test #7
    [Fact]
    public async Task FollowerGetsAppendEntriesMessageElectionTimerResets()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.StateMachine = new();
        var followerNode = new Node([leaderNode], 1, client);
        followerNode.LeaderId = 2;

        // Act
        await followerNode.RequestAppendEntriesRPC(1, leaderNode.Id, 0, 0, new List<Log>(), 0);
        Thread.Sleep(100);

        // Assert
        followerNode.State.Should().Be(State.Follower);
    }


    // Test #8
    [Fact]
    public void CandidateGetsMajorityVotes()
    {
        // Arrange
        var testNode = new Node(1);

        // Act
        testNode.StartElection();
        Thread.Sleep(320);
        testNode.DetermineWinner();

        // Assert
        testNode.State.Should().Be(State.Leader);
    }

    // Test #9
    [Fact]
    public async Task UnresponsiveNodeStillGiveCandidateLeadershipStatus()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var followerNode1 = Substitute.For<INode>();
        followerNode1.Id = 1;
        var followerNode2 = Substitute.For<INode>();
        followerNode2.Id = 2;
        var leaderNode = new Node([followerNode1, followerNode2], 3, client);

        // Act
        leaderNode.StartElection();
        await leaderNode.CastVoteRPC(leaderNode.Term, true);
        leaderNode.DetermineWinner();

        // Assert
        leaderNode.State.Should().Be(State.Leader);
    }

    // Test #10
    [Fact]
    public async Task FollowerHasntVotedYetRespondsWithYesForRequestToVote()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        var followerNode = new Node([leaderNode], 1, client);

        // Act
        await followerNode.RequestVoteRPC(1, 2);

        // Assert
        await leaderNode.Received().CastVoteRPC(1, true);
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

    // Test #12
    [Fact]
    public async Task CandidateReceivesMessageFromNodeWithLaterTermShouldBecomeFollower()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.StateMachine = new();

        var candidateNode = new Node([leaderNode], 1, client);
        candidateNode.State = State.Candidate;
        candidateNode.Term = 1;
        candidateNode.LeaderId = 2;

        // Act
        await candidateNode.RequestAppendEntriesRPC(2, leaderNode.Id, 0, 0, new List<Log>(), 0);

        // Assert
        candidateNode.State.Should().Be(State.Follower);
    }

    // Test # 13
    [Fact]
    public async Task CandidateReceivesMessageFromNodeWithAnEqualTermShouldBecomeFollower()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.StateMachine = new();

        var candidateNode = new Node([leaderNode], 1, client);
        candidateNode.State = State.Candidate;
        candidateNode.Term = 1;
        candidateNode.LeaderId = 2;

        // Act
        await candidateNode.RequestAppendEntriesRPC(1, leaderNode.Id, 0, 0, new List<Log>(), 0);

        // Assert
        candidateNode.State.Should().Be(State.Follower);
    }

    // Test #14
    [Fact]
    public async Task FollowerWontVoteTwiceForSameTerm()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        var followerNode = new Node([leaderNode], 1, client);

        // Act
        await followerNode.RequestVoteRPC(1, 2);
        await followerNode.RequestVoteRPC(1, 2);

        // Assert
        await leaderNode.Received().CastVoteRPC(1, false);
    }

    // Test #15
    [Fact]
    public async Task FutureTermMakesThemVoteYes()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        var followerNode = new Node([leaderNode], 1, client);

        // Act
        await followerNode.RequestVoteRPC(1, 2);
        await followerNode.RequestVoteRPC(2, 2);

        // Assert
        await leaderNode.Received().CastVoteRPC(2, true);
    }

    // Test #16
    [Fact]
    public void WhenElectionTimerExpiresInsideElectionAnotherElectionStarts()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var testNode1 = Substitute.For<INode>();
        testNode1.Id = 1;
        var testNode2 = Substitute.For<INode>();
        testNode2.Id = 2;
        List<INode> otherNodes = new List<INode>() { testNode1, testNode2 };
        var testNode = new Node(otherNodes, 3, client);

        // Act
        testNode.StartElection();
        var result = testNode.Term;
        Thread.Sleep(600);

        // Assert
        testNode.Term.Should().NotBe(result);
    }

    // Test #17
    [Fact]
    public async Task FollowerNodeRecievesAnAppendEntriesRequestItResponds()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;
        leaderNode.StateMachine = new();

        var followerNode = new Node([leaderNode], 1, client);
        followerNode.LeaderId = 2;

        // Act
        await followerNode.RequestAppendEntriesRPC(1, leaderNode.Id, 0, 0, new List<Log>(), 0);

        // Assert
        await leaderNode.Received().ConfirmAppendEntriesRPC(1, 1);
    }

    // Test #18
    [Fact]
    public async Task CandidateRecievesOldAppendEntriesFromPreviousTermRejects()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var candidateNode = Substitute.For<INode>();
        candidateNode.Id = 1;

        var leaderNode = new Node([candidateNode], 2, client);
        leaderNode.Term = 1;

        // Act
        leaderNode.SendAppendEntriesRPC(2, 1);

        // Assert
        await candidateNode.DidNotReceive().RequestAppendEntriesRPC(leaderNode.Term, leaderNode.Id, 0, 0, new List<Log>(), 0);
    }

    // Test #19
    [Fact]
    public void WhenACandidateWinsAnElectionItImmediatelySendsAHeartbeat()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var otherNode = Substitute.For<INode>();
        otherNode.Id = 1;
        var leaderNode = new Node([otherNode], 2, client);

        // Act
        leaderNode.StartElection();
        Thread.Sleep(320);
        leaderNode.DetermineWinner();

        // Assert
        otherNode.Received().RequestAppendEntriesRPC(leaderNode.Term, leaderNode.Id, 0, 0, Arg.Any<List<Log>>(), 0);
    }
}