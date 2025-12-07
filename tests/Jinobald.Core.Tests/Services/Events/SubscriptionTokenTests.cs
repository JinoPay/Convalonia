using Jinobald.Core.Services.Events;

namespace Jinobald.Core.Tests.Services.Events;

public class SubscriptionTokenTests
{
    [Fact]
    public void Constructor_ShouldGenerateUniqueId()
    {
        // Arrange & Act
        var token1 = new SubscriptionToken(typeof(TestEvent), _ => { });
        var token2 = new SubscriptionToken(typeof(TestEvent), _ => { });

        // Assert
        Assert.NotEqual(token1.Id, token2.Id);
    }

    [Fact]
    public void Constructor_ShouldSetEventType()
    {
        // Arrange & Act
        var token = new SubscriptionToken(typeof(TestEvent), _ => { });

        // Assert
        Assert.Equal(typeof(TestEvent), token.EventType);
    }

    [Fact]
    public void Dispose_ShouldCallUnsubscribeAction()
    {
        // Arrange
        var unsubscribeCalled = false;
        SubscriptionToken? capturedToken = null;
        var token = new SubscriptionToken(typeof(TestEvent), t =>
        {
            unsubscribeCalled = true;
            capturedToken = t;
        });

        // Act
        token.Dispose();

        // Assert
        Assert.True(unsubscribeCalled);
        Assert.Same(token, capturedToken);
    }

    [Fact]
    public void Dispose_ShouldOnlyCallUnsubscribeOnce()
    {
        // Arrange
        var callCount = 0;
        var token = new SubscriptionToken(typeof(TestEvent), _ => callCount++);

        // Act
        token.Dispose();
        token.Dispose();
        token.Dispose();

        // Assert
        Assert.Equal(1, callCount);
    }

    private class TestEvent : PubSubEvent { }
}
