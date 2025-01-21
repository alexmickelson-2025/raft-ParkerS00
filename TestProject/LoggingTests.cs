using FluentAssertions;
using Raft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject;

public class LoggingTests
{
    //
    [Fact]
    public async Task LeaderAppendsMessageToItsLogs()
    {
        // Arrange
        var leaderNode = new Node(1);

        // Act
        await leaderNode.ConfirmAppendEntriesRPC();

        // Assert
        leaderNode.logs.Count.Should().Be(1);
    }
}
