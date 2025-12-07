using System.Collections.Concurrent;
using Avalonia.Threading;
using Jinobald.Core.Services.Events;

namespace Jinobald.Avalonia.Services.Events;

/// <summary>
///     이벤트 집계기 구현
///     Thread-safe한 Pub/Sub 패턴 구현
/// </summary>
public sealed class EventAggregator : IEventAggregator
{
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<Type, List<Subscription>> _subscriptions = new();
    private readonly ConcurrentDictionary<Type, object> _eventInstances = new();

    public PubSubEvent<TEvent> GetEvent<TEvent>() where TEvent : PubSubEvent
    {
        var eventType = typeof(TEvent);
        if (_eventInstances.TryGetValue(eventType, out var existingEvent))
            return (PubSubEvent<TEvent>)existingEvent;

        var newEvent = new PubSubEvent<TEvent>(this);
        _eventInstances[eventType] = newEvent;
        return newEvent;
    }

    public SubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : PubSubEvent
    {
        return Subscribe(handler, ThreadOption.UIThread);
    }

    public SubscriptionToken Subscribe<TEvent>(Action<TEvent> handler, ThreadOption threadOption) where TEvent : PubSubEvent
    {
        return Subscribe(handler, threadOption, keepSubscriberReferenceAlive: true);
    }

    public SubscriptionToken Subscribe<TEvent>(Action<TEvent> handler, ThreadOption threadOption, bool keepSubscriberReferenceAlive) where TEvent : PubSubEvent
    {
        return SubscribeInternal<TEvent>(handler, null, threadOption, keepSubscriberReferenceAlive);
    }

    public SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : PubSubEvent
    {
        return Subscribe(handler, ThreadOption.UIThread, keepSubscriberReferenceAlive: true);
    }

    public SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler, ThreadOption threadOption)
        where TEvent : PubSubEvent
    {
        return Subscribe(handler, threadOption, keepSubscriberReferenceAlive: true);
    }

    public SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
        where TEvent : PubSubEvent
    {
        return SubscribeInternal<TEvent>(handler, null, threadOption, keepSubscriberReferenceAlive);
    }

    public SubscriptionToken Subscribe<TEvent>(
        Action<TEvent> handler,
        Predicate<TEvent> filter,
        ThreadOption threadOption = ThreadOption.UIThread,
        bool keepSubscriberReferenceAlive = true) where TEvent : PubSubEvent
    {
        ArgumentNullException.ThrowIfNull(filter);
        return SubscribeInternal<TEvent>(handler, filter, threadOption, keepSubscriberReferenceAlive);
    }

    public SubscriptionToken Subscribe<TEvent>(
        Func<TEvent, Task> handler,
        Predicate<TEvent> filter,
        ThreadOption threadOption = ThreadOption.UIThread,
        bool keepSubscriberReferenceAlive = true) where TEvent : PubSubEvent
    {
        ArgumentNullException.ThrowIfNull(filter);
        return SubscribeInternal<TEvent>(handler, filter, threadOption, keepSubscriberReferenceAlive);
    }

    public async Task PublishAsync<TEvent>(TEvent eventData) where TEvent : PubSubEvent
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.TryGetValue(eventType, out var subscriptions))
            return;

        List<Subscription> subscriptionsCopy;
        List<Subscription>? deadSubscriptions = null;

        lock (_lock)
        {
            subscriptionsCopy = subscriptions.ToList();
        }

        var tasks = new List<Task>();

        foreach (var subscription in subscriptionsCopy)
        {
            // Weak Reference가 죽었는지 확인
            if (!subscription.IsAlive)
            {
                deadSubscriptions ??= new List<Subscription>();
                deadSubscriptions.Add(subscription);
                continue;
            }

            var task = ExecuteHandlerAsync(subscription, eventData);
            if (task != null) tasks.Add(task);
        }

        // 죽은 구독 제거
        if (deadSubscriptions != null)
        {
            lock (_lock)
            {
                foreach (var dead in deadSubscriptions)
                    subscriptions.Remove(dead);
            }
        }

        if (tasks.Count > 0) await Task.WhenAll(tasks);
    }

    public void Publish<TEvent>(TEvent eventData) where TEvent : PubSubEvent
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.TryGetValue(eventType, out var subscriptions))
            return;

        List<Subscription> subscriptionsCopy;
        List<Subscription>? deadSubscriptions = null;

        lock (_lock)
        {
            subscriptionsCopy = subscriptions.ToList();
        }

        foreach (var subscription in subscriptionsCopy)
        {
            // Weak Reference가 죽었는지 확인
            if (!subscription.IsAlive)
            {
                deadSubscriptions ??= new List<Subscription>();
                deadSubscriptions.Add(subscription);
                continue;
            }

            ExecuteHandler(subscription, eventData);
        }

        // 죽은 구독 제거
        if (deadSubscriptions != null)
        {
            lock (_lock)
            {
                foreach (var dead in deadSubscriptions)
                    subscriptions.Remove(dead);
            }
        }
    }

    public void Unsubscribe(SubscriptionToken token)
    {
        if (!_subscriptions.TryGetValue(token.EventType, out var subscriptions))
            return;

        lock (_lock)
        {
            subscriptions.RemoveAll(s => s.Token.Id == token.Id);
        }
    }

    private SubscriptionToken SubscribeInternal<TEvent>(Delegate handler, Delegate? filter, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
        where TEvent : PubSubEvent
    {
        var eventType = typeof(TEvent);
        var token = new SubscriptionToken(eventType, Unsubscribe);

        var subscription = new Subscription
        {
            Token = token,
            Handler = handler,
            Filter = filter,
            ThreadOption = threadOption,
            IsWeakReference = !keepSubscriberReferenceAlive,
            WeakHandler = keepSubscriberReferenceAlive ? null : new WeakReference<Delegate>(handler)
        };

        lock (_lock)
        {
            var subscriptions = _subscriptions.GetOrAdd(eventType, _ => new List<Subscription>());
            subscriptions.Add(subscription);
        }

        return token;
    }

    private async Task InvokeHandlerAsync<TEvent>(Delegate handler, TEvent eventData) where TEvent : PubSubEvent
    {
        try
        {
            if (handler is Action<TEvent> syncHandler)
                syncHandler(eventData);
            else if (handler is Func<TEvent, Task> asyncHandler) await asyncHandler(eventData);
        }
        catch
        {
            // Suppress exceptions in event handlers to prevent breaking the event flow
        }
    }

    private Task? ExecuteHandlerAsync<TEvent>(Subscription subscription, TEvent eventData) where TEvent : PubSubEvent
    {
        try
        {
            var handler = subscription.GetHandler();
            if (handler == null)
                return null;

            // Check filter predicate
            if (subscription.Filter is Predicate<TEvent> filter && !filter(eventData))
                return null;

            return subscription.ThreadOption switch
            {
                ThreadOption.PublisherThread => InvokeHandlerAsync(handler, eventData),

                ThreadOption.UIThread => Dispatcher.UIThread.InvokeAsync(async () =>
                    await InvokeHandlerAsync(handler, eventData)),

                ThreadOption.BackgroundThread => Task.Run(() => InvokeHandlerAsync(handler, eventData)),

                _ => null
            };
        }
        catch
        {
            return Task.CompletedTask;
        }
    }

    private void ExecuteHandler<TEvent>(Subscription subscription, TEvent eventData) where TEvent : PubSubEvent
    {
        try
        {
            var handler = subscription.GetHandler();
            if (handler == null)
                return;

            // Check filter predicate
            if (subscription.Filter is Predicate<TEvent> filter && !filter(eventData))
                return;

            switch (subscription.ThreadOption)
            {
                case ThreadOption.PublisherThread:
                    InvokeHandler(handler, eventData);
                    break;

                case ThreadOption.UIThread:
                    Dispatcher.UIThread.Post(() => InvokeHandler(handler, eventData));
                    break;

                case ThreadOption.BackgroundThread:
                    ThreadPool.QueueUserWorkItem(_ => InvokeHandler(handler, eventData));
                    break;
            }
        }
        catch
        {
            // Suppress exceptions
        }
    }

    private void InvokeHandler<TEvent>(Delegate handler, TEvent eventData) where TEvent : PubSubEvent
    {
        try
        {
            if (handler is Action<TEvent> syncHandler)
                syncHandler(eventData);
            else if (handler is Func<TEvent, Task> asyncHandler) _ = asyncHandler(eventData);
        }
        catch
        {
            // Suppress exceptions
        }
    }

    private sealed class Subscription
    {
        public required Delegate Handler { get; init; }
        public required SubscriptionToken Token { get; init; }
        public Delegate? Filter { get; init; }
        public ThreadOption ThreadOption { get; init; }
        public bool IsWeakReference { get; init; }
        public WeakReference<Delegate>? WeakHandler { get; init; }

        /// <summary>
        ///     현재 유효한 핸들러 가져오기 (Weak Reference인 경우 null 반환 가능)
        /// </summary>
        public Delegate? GetHandler()
        {
            if (!IsWeakReference)
                return Handler;

            if (WeakHandler != null && WeakHandler.TryGetTarget(out var handler))
                return handler;

            return null;
        }

        /// <summary>
        ///     구독이 유효한지 확인 (Weak Reference가 살아있는지)
        /// </summary>
        public bool IsAlive => !IsWeakReference || (WeakHandler?.TryGetTarget(out _) ?? false);
    }
}
