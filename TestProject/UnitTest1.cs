using Raft;

namespace TestProject;

public class UnitTest1
{
    [Fact]
    public void WhenNodeIsCreatedItIsAFollower()
    {
        // Arrange
        Node testNode = new Node();

        // Act 
        var result = testNode.IsFollower;

        // Assert
        Assert.True(result);
    }
}