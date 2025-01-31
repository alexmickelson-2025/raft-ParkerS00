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
        followerNode.Received(1).RequestAppendEntriesRPC(Arg.Is<RequestAppendEntriesData>(dto => dto.Term == 0 &&
                                                                                                dto.LeaderId == 2 &&
                                                                                                dto.PrevLogIndex == 0 &&
                                                                                                dto.PrevLogTerm == 0 &&
                                                                                                dto.LeaderCommit == 0));
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
        followerNode.Received().RequestAppendEntriesRPC(Arg.Is<RequestAppendEntriesData>(dto => dto.Term == 0 &&
                                                                                                dto.LeaderId == 2 &&
                                                                                                dto.PrevLogIndex == 0 &&
                                                                                                dto.PrevLogTerm == 0 &&
                                                                                                dto.LeaderCommit == 0));

        leaderNode.Pause();
        Thread.Sleep(400);
        leaderNode.UnPause();
        Thread.Sleep(48);

        // Assert
        followerNode.Received().RequestAppendEntriesRPC(Arg.Is<RequestAppendEntriesData>(dto => dto.Term == 0 &&
                                                                                                dto.LeaderId == 2 &&
                                                                                                dto.PrevLogIndex == 0 &&
                                                                                                dto.PrevLogTerm == 0 &&
                                                                                                dto.LeaderCommit == 0));
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
