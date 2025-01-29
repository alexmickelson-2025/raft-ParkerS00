using ClassLibrary;
using FluentAssertions;
using NSubstitute;
using Raft;

namespace TestProject;

public class DebuggingTests
{
    [Fact]
    public void WhenLeaderStartsCommitIndexShouldBe0()
    {
        // Arrange
        var leaderNode = new Node(1);

        // Act
        leaderNode.BecomeLeader();
        Thread.Sleep(60);

        // Assert
        leaderNode.LeaderCommitIndex.Should().Be(0);
        leaderNode.NextIndex.Should().Be(0);
    }

    [Fact]
    public void WhenLeaderSendsHeatbeatWithNoLogsToFollowersCommitIndexShouldBe0()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode = Substitute.For<INode>();
        followerNode.Id = 1;

        var leaderNode = new Node([followerNode], 1, client);

        // Act
        leaderNode.BecomeLeader();
        Thread.Sleep(60);

        // Assert
        leaderNode.LeaderCommitIndex.Should().Be(0);
        leaderNode.logs.Count.Should().Be(0);
        leaderNode.NextIndex.Should().Be(0);
    }

    [Fact]
    public void WhenLeaderSendsHeartbeatWithOneLogCommitIndexShouldGoUpTo1()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode = Substitute.For<INode>();
        followerNode.Id = 1;

        var leaderNode = new Node([followerNode], 1, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("test");
        Thread.Sleep(70);

        // Assert
        leaderNode.LeaderCommitIndex.Should().Be(1);
        leaderNode.logs.Count().Should().Be(1);
        leaderNode.NextIndex.Should().Be(1);
    }

    
}
