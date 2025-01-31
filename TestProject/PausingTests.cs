using ClassLibrary;
using FluentAssertions;
using NSubstitute;
using Raft;

namespace TestProject;

public class PausingTests
{
    [Fact]
    public void LeaderWithElectionLoopGetsPausedFollowerDontGetAHeartbeat()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var followerNode = Substitute.For<INode>();
        followerNode.Id = 1;

        var leaderNode = new Node([followerNode], 2, client);
        leaderNode.BecomeLeader();

        // Act
        leaderNode.Pause();
        Thread.Sleep(400);

        // Assert
        followerNode.Received(1).RequestAppendEntriesRPC(0, 2, 0, 0, Arg.Any<List<Log>>(), 0);
    }

    [Fact]
    public void LeaderGetsPausedThenGetsUnpausedAndSendsHeartbeatAgain()
    {
        // Arrange
        var client = Substitute.For<IClient>();
        var followerNode = Substitute.For<INode>();
        followerNode.Id = 1;

        var leaderNode = new Node([followerNode], 2, client);
        leaderNode.BecomeLeader();

        // Act
        followerNode.Received().RequestAppendEntriesRPC(0, 2, 0, 0, Arg.Any<List<Log>>(), 0);

        leaderNode.Pause();
        Thread.Sleep(400);
        leaderNode.UnPause();
        Thread.Sleep(48);

        // Assert
        followerNode.Received().RequestAppendEntriesRPC(0, 2, 0, 0, Arg.Any<List<Log>>(), 0);
    }

    [Fact]
    public void FollowerGetsPausedItDoesNotTimeOutToBecomeCandidate()
    {
        // Arrange
        var followerNode = new Node(1);

        // Act
        followerNode.Pause();
        Thread.Sleep(400);

        // Assert
        followerNode.State.Should().Be(State.Follower);
    }

    [Fact]
    public void FollowerGetsUnpausedShouldBecomeCandidate()
    {
        // Arrange
        var followerNode = new Node(1);

        // Act
        followerNode.Pause();
        Thread.Sleep(400);
        followerNode.UnPause();
        Thread.Sleep(300);

        // Assert
        followerNode.State.Should().Be(State.Candidate);
    }
}
