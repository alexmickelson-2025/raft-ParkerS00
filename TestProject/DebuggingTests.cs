﻿using ClassLibrary;
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
        leaderNode.CommitIndex.Should().Be(0);
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
        leaderNode.CommitIndex.Should().Be(0);
        leaderNode.logs.Count.Should().Be(0);
        leaderNode.NextIndex.Should().Be(0);
    }

    [Fact]
    public async Task WhenLeaderSendsHeartbeatWithOneLogCommitIndexShouldGoUpTo1()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var followerNode = Substitute.For<INode>();
        followerNode.Id = 1;

        var leaderNode = new Node([followerNode], 1, client);

        // Act
        leaderNode.BecomeLeader();
        leaderNode.RecieveClientCommand("key", "value");
        await leaderNode.ConfirmAppendEntriesRPC(new ConfirmAppendEntriesData(0, 1, true, 1));

        // Assert
        leaderNode.CommitIndex.Should().Be(1);
        leaderNode.logs.Count().Should().Be(1);
        leaderNode.NextIndex.Should().Be(1);

        Thread.Sleep(70);
        leaderNode.CommitIndex.Should().Be(1);
        leaderNode.logs.Count().Should().Be(1);
        leaderNode.NextIndex.Should().Be(1);
    }

    [Fact]
    public async Task FollowerNodeShouldOnlyCommitLogOnce()
    {
        // Arrange
        var client = Substitute.For<IClient>();

        var leaderNode = Substitute.For<INode>();
        leaderNode.Id = 2;

        var followerNode = new Node([leaderNode], 1, client);

        var log = new Log(1, "key", "value");
        var logs = new List<Log>
        {
            log
        };

        // Act
        await followerNode.RequestAppendEntriesRPC(new RequestAppendEntriesData(1, 2, 0, 0, logs, 0));
        await followerNode.RequestAppendEntriesRPC(new RequestAppendEntriesData(1, 2, 0, 1, new List<Log>(), 0));

        // Assert
        followerNode.logs.Count().Should().Be(1);
    }
}
