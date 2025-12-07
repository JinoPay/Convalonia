using Jinobald.Core.Services.Events;
using Xunit;

namespace Jinobald.Core.Tests.Services.Events;

public class EventFilterPredicateTests
{
    public class TestEvent : PubSubEvent
    {
        public int Value { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mock EventAggregator for testing filter functionality
    /// </summary>
    private class MockEventAggregator : IEventAggregator
    {
        private readonly Dictionary<Type, List<(Delegate Handler, Delegate? Filter, ThreadOption ThreadOption)>> _subscriptions = new();

        private readonly Dictionary<Type, object> _eventInstances = new();

        public PubSubEvent<TEvent> GetEvent<TEvent>() where TEvent : PubSubEvent
        {
            var eventType = typeof(TEvent);
            if (_eventInstances.TryGetValue(eventType, out var existingEvent))
                return (PubSubEvent<TEvent>)existingEvent;

            // Use reflection to create PubSubEvent<TEvent> since constructor is internal
            var constructor = typeof(PubSubEvent<TEvent>).GetConstructor(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, new[] { typeof(IEventAggregator) }, null);

            var newEvent = (PubSubEvent<TEvent>)constructor!.Invoke(new object[] { this });
            _eventInstances[eventType] = newEvent;
            return newEvent;
        }

        public SubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : PubSubEvent
            => Subscribe(handler, ThreadOption.PublisherThread);

        public SubscriptionToken Subscribe<TEvent>(Action<TEvent> handler, ThreadOption threadOption) where TEvent : PubSubEvent
            => Subscribe(handler, threadOption, true);

        public SubscriptionToken Subscribe<TEvent>(Action<TEvent> handler, ThreadOption threadOption, bool keepSubscriberReferenceAlive) where TEvent : PubSubEvent
        {
            var token = new SubscriptionToken(typeof(TEvent), Unsubscribe);
            if (!_subscriptions.ContainsKey(typeof(TEvent)))
                _subscriptions[typeof(TEvent)] = new();
            _subscriptions[typeof(TEvent)].Add((handler, null, threadOption));
            return token;
        }

        public SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : PubSubEvent
            => Subscribe(handler, ThreadOption.PublisherThread);

        public SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler, ThreadOption threadOption) where TEvent : PubSubEvent
            => Subscribe(handler, threadOption, true);

        public SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler, ThreadOption threadOption, bool keepSubscriberReferenceAlive) where TEvent : PubSubEvent
        {
            var token = new SubscriptionToken(typeof(TEvent), Unsubscribe);
            if (!_subscriptions.ContainsKey(typeof(TEvent)))
                _subscriptions[typeof(TEvent)] = new();
            _subscriptions[typeof(TEvent)].Add((handler, null, threadOption));
            return token;
        }

        public SubscriptionToken Subscribe<TEvent>(
            Action<TEvent> handler,
            Predicate<TEvent> filter,
            ThreadOption threadOption = ThreadOption.UIThread,
            bool keepSubscriberReferenceAlive = true) where TEvent : PubSubEvent
        {
            var token = new SubscriptionToken(typeof(TEvent), Unsubscribe);
            if (!_subscriptions.ContainsKey(typeof(TEvent)))
                _subscriptions[typeof(TEvent)] = new();
            _subscriptions[typeof(TEvent)].Add((handler, filter, threadOption));
            return token;
        }

        public SubscriptionToken Subscribe<TEvent>(
            Func<TEvent, Task> handler,
            Predicate<TEvent> filter,
            ThreadOption threadOption = ThreadOption.UIThread,
            bool keepSubscriberReferenceAlive = true) where TEvent : PubSubEvent
        {
            var token = new SubscriptionToken(typeof(TEvent), Unsubscribe);
            if (!_subscriptions.ContainsKey(typeof(TEvent)))
                _subscriptions[typeof(TEvent)] = new();
            _subscriptions[typeof(TEvent)].Add((handler, filter, threadOption));
            return token;
        }

        public void Publish<TEvent>(TEvent eventData) where TEvent : PubSubEvent
        {
            if (!_subscriptions.TryGetValue(typeof(TEvent), out var subs))
                return;

            foreach (var (handler, filter, _) in subs)
            {
                // Check filter
                if (filter is Predicate<TEvent> predicate && !predicate(eventData))
                    continue;

                if (handler is Action<TEvent> action)
                    action(eventData);
            }
        }

        public async Task PublishAsync<TEvent>(TEvent eventData) where TEvent : PubSubEvent
        {
            if (!_subscriptions.TryGetValue(typeof(TEvent), out var subs))
                return;

            foreach (var (handler, filter, _) in subs)
            {
                // Check filter
                if (filter is Predicate<TEvent> predicate && !predicate(eventData))
                    continue;

                if (handler is Action<TEvent> action)
                    action(eventData);
                else if (handler is Func<TEvent, Task> asyncAction)
                    await asyncAction(eventData);
            }
        }

        public void Unsubscribe(SubscriptionToken token)
        {
            if (_subscriptions.TryGetValue(token.EventType, out var subs))
                subs.Clear();
        }
    }

    [Fact]
    public void Subscribe_WithFilter_ShouldOnlyReceiveMatchingEvents()
    {
        // Arrange
        var aggregator = new MockEventAggregator();
        var receivedValues = new List<int>();

        // Subscribe with filter: only even numbers
        aggregator.Subscribe<TestEvent>(
            e => receivedValues.Add(e.Value),
            e => e.Value % 2 == 0);

        // Act
        aggregator.Publish(new TestEvent { Value = 1 });
        aggregator.Publish(new TestEvent { Value = 2 });
        aggregator.Publish(new TestEvent { Value = 3 });
        aggregator.Publish(new TestEvent { Value = 4 });

        // Assert
        Assert.Equal(2, receivedValues.Count);
        Assert.Contains(2, receivedValues);
        Assert.Contains(4, receivedValues);
    }

    [Fact]
    public void Subscribe_WithCategoryFilter_ShouldFilterByCategory()
    {
        // Arrange
        var aggregator = new MockEventAggregator();
        var receivedEvents = new List<TestEvent>();

        // Subscribe with filter: only "important" category
        aggregator.Subscribe<TestEvent>(
            e => receivedEvents.Add(e),
            e => e.Category == "important");

        // Act
        aggregator.Publish(new TestEvent { Value = 1, Category = "normal" });
        aggregator.Publish(new TestEvent { Value = 2, Category = "important" });
        aggregator.Publish(new TestEvent { Value = 3, Category = "normal" });
        aggregator.Publish(new TestEvent { Value = 4, Category = "important" });

        // Assert
        Assert.Equal(2, receivedEvents.Count);
        Assert.All(receivedEvents, e => Assert.Equal("important", e.Category));
    }

    [Fact]
    public async Task SubscribeAsync_WithFilter_ShouldOnlyReceiveMatchingEvents()
    {
        // Arrange
        var aggregator = new MockEventAggregator();
        var receivedValues = new List<int>();

        // Subscribe async with filter
        aggregator.Subscribe<TestEvent>(
            async e =>
            {
                await Task.Yield();
                receivedValues.Add(e.Value);
            },
            e => e.Value > 5);

        // Act
        await aggregator.PublishAsync(new TestEvent { Value = 3 });
        await aggregator.PublishAsync(new TestEvent { Value = 7 });
        await aggregator.PublishAsync(new TestEvent { Value = 5 });
        await aggregator.PublishAsync(new TestEvent { Value = 10 });

        // Assert
        Assert.Equal(2, receivedValues.Count);
        Assert.Contains(7, receivedValues);
        Assert.Contains(10, receivedValues);
    }

    [Fact]
    public void MultipleSubscribers_WithDifferentFilters_ShouldReceiveCorrectEvents()
    {
        // Arrange
        var aggregator = new MockEventAggregator();
        var evenValues = new List<int>();
        var largeValues = new List<int>();

        // Subscribe for even numbers
        aggregator.Subscribe<TestEvent>(
            e => evenValues.Add(e.Value),
            e => e.Value % 2 == 0);

        // Subscribe for values > 5
        aggregator.Subscribe<TestEvent>(
            e => largeValues.Add(e.Value),
            e => e.Value > 5);

        // Act
        aggregator.Publish(new TestEvent { Value = 2 });  // even only
        aggregator.Publish(new TestEvent { Value = 7 });  // large only
        aggregator.Publish(new TestEvent { Value = 8 });  // both
        aggregator.Publish(new TestEvent { Value = 3 });  // neither

        // Assert
        Assert.Equal(new[] { 2, 8 }, evenValues);
        Assert.Equal(new[] { 7, 8 }, largeValues);
    }

    [Fact]
    public void Subscribe_WithFalseFilter_ShouldNotReceiveAnyEvents()
    {
        // Arrange
        var aggregator = new MockEventAggregator();
        var receivedCount = 0;

        // Subscribe with filter that always returns false
        aggregator.Subscribe<TestEvent>(
            _ => receivedCount++,
            _ => false);

        // Act
        aggregator.Publish(new TestEvent { Value = 1 });
        aggregator.Publish(new TestEvent { Value = 2 });

        // Assert
        Assert.Equal(0, receivedCount);
    }

    [Fact]
    public void Subscribe_WithTrueFilter_ShouldReceiveAllEvents()
    {
        // Arrange
        var aggregator = new MockEventAggregator();
        var receivedCount = 0;

        // Subscribe with filter that always returns true
        aggregator.Subscribe<TestEvent>(
            _ => receivedCount++,
            _ => true);

        // Act
        aggregator.Publish(new TestEvent { Value = 1 });
        aggregator.Publish(new TestEvent { Value = 2 });
        aggregator.Publish(new TestEvent { Value = 3 });

        // Assert
        Assert.Equal(3, receivedCount);
    }

    [Fact]
    public void PubSubEvent_Subscribe_WithFilter_ShouldWork()
    {
        // Arrange
        var aggregator = new MockEventAggregator();
        var receivedCount = 0;
        var evt = aggregator.GetEvent<TestEvent>();

        // Subscribe through PubSubEvent
        evt.Subscribe(
            _ => receivedCount++,
            e => e.Value > 10);

        // Act (publish through aggregator directly since PubSubEvent.Publish uses aggregator)
        aggregator.Publish(new TestEvent { Value = 5 });
        aggregator.Publish(new TestEvent { Value = 15 });

        // Assert
        Assert.Equal(1, receivedCount);
    }
}
