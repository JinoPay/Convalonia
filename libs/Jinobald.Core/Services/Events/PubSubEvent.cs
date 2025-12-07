namespace Jinobald.Core.Services.Events;

/// <summary>
///     이벤트 집계기에서 사용할 수 있는 이벤트의 베이스 클래스
///     모든 이벤트는 이 클래스를 상속해야 함
/// </summary>
public class PubSubEvent
{
}

/// <summary>
///     타입 안전한 이벤트 구독/발행을 제공하는 제네릭 이벤트 클래스
/// </summary>
/// <typeparam name="TEvent">이벤트 타입 (PubSubEvent 상속 필수)</typeparam>
public sealed class PubSubEvent<TEvent> where TEvent : PubSubEvent
{
    private readonly IEventAggregator _eventAggregator;

    internal PubSubEvent(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    /// <summary>
    ///     이벤트 구독 (기본: UI 스레드)
    /// </summary>
    /// <param name="handler">이벤트 핸들러</param>
    /// <returns>구독 해제를 위한 토큰</returns>
    public SubscriptionToken Subscribe(Action<TEvent> handler)
    {
        return _eventAggregator.Subscribe(handler);
    }

    /// <summary>
    ///     이벤트 구독 (스레드 옵션 지정)
    /// </summary>
    public SubscriptionToken Subscribe(Action<TEvent> handler, ThreadOption threadOption)
    {
        return _eventAggregator.Subscribe(handler, threadOption);
    }

    /// <summary>
    ///     비동기 이벤트 구독 (기본: UI 스레드)
    /// </summary>
    public SubscriptionToken Subscribe(Func<TEvent, Task> handler)
    {
        return _eventAggregator.Subscribe(handler);
    }

    /// <summary>
    ///     비동기 이벤트 구독 (스레드 옵션 지정)
    /// </summary>
    public SubscriptionToken Subscribe(Func<TEvent, Task> handler, ThreadOption threadOption)
    {
        return _eventAggregator.Subscribe(handler, threadOption);
    }

    /// <summary>
    ///     이벤트 구독 (필터 조건 지정)
    ///     필터 조건을 만족하는 이벤트만 핸들러로 전달됩니다.
    /// </summary>
    /// <param name="handler">이벤트 핸들러</param>
    /// <param name="filter">이벤트 필터 (true 반환 시에만 핸들러 호출)</param>
    /// <param name="threadOption">실행 스레드 옵션</param>
    /// <param name="keepSubscriberReferenceAlive">참조 유지 옵션</param>
    public SubscriptionToken Subscribe(
        Action<TEvent> handler,
        Predicate<TEvent> filter,
        ThreadOption threadOption = ThreadOption.UIThread,
        bool keepSubscriberReferenceAlive = true)
    {
        return _eventAggregator.Subscribe(handler, filter, threadOption, keepSubscriberReferenceAlive);
    }

    /// <summary>
    ///     비동기 이벤트 구독 (필터 조건 지정)
    /// </summary>
    public SubscriptionToken Subscribe(
        Func<TEvent, Task> handler,
        Predicate<TEvent> filter,
        ThreadOption threadOption = ThreadOption.UIThread,
        bool keepSubscriberReferenceAlive = true)
    {
        return _eventAggregator.Subscribe(handler, filter, threadOption, keepSubscriberReferenceAlive);
    }

    /// <summary>
    ///     이벤트를 발행
    /// </summary>
    public void Publish(TEvent eventData)
    {
        _eventAggregator.Publish(eventData);
    }

    /// <summary>
    ///     이벤트를 비동기로 발행
    /// </summary>
    public Task PublishAsync(TEvent eventData)
    {
        return _eventAggregator.PublishAsync(eventData);
    }
}
