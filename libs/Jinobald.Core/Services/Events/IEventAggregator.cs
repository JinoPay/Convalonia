namespace Jinobald.Core.Services.Events;

/// <summary>
///     이벤트 핸들러 실행 스레드 옵션
/// </summary>
public enum ThreadOption
{
    /// <summary>
    ///     이벤트를 발행한 스레드에서 실행 (동기)
    /// </summary>
    PublisherThread,

    /// <summary>
    ///     UI 스레드에서 실행 (Dispatcher.UIThread)
    /// </summary>
    UIThread,

    /// <summary>
    ///     백그라운드 스레드에서 실행 (ThreadPool)
    /// </summary>
    BackgroundThread
}

/// <summary>
///     이벤트 집계기 인터페이스
///     Pub/Sub 패턴으로 느슨한 결합의 컴포넌트 간 통신을 지원
///     모든 이벤트는 PubSubEvent를 상속해야 함
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    ///     특정 타입의 이벤트를 가져옴 (Prism 스타일)
    /// </summary>
    /// <typeparam name="TEvent">이벤트 타입 (PubSubEvent 상속 필수)</typeparam>
    /// <returns>타입 안전한 이벤트 객체</returns>
    PubSubEvent<TEvent> GetEvent<TEvent>() where TEvent : PubSubEvent;

    /// <summary>
    ///     이벤트 구독 (기본: UI 스레드)
    /// </summary>
    /// <typeparam name="TEvent">이벤트 타입 (PubSubEvent 상속 필수)</typeparam>
    /// <param name="handler">이벤트 핸들러</param>
    /// <returns>구독 해제를 위한 토큰</returns>
    SubscriptionToken Subscribe<TEvent>(Action<TEvent> handler) where TEvent : PubSubEvent;

    /// <summary>
    ///     이벤트 구독 (스레드 옵션 지정)
    /// </summary>
    SubscriptionToken Subscribe<TEvent>(Action<TEvent> handler, ThreadOption threadOption) where TEvent : PubSubEvent;

    /// <summary>
    ///     이벤트 구독 (스레드 옵션 및 참조 유지 옵션 지정)
    /// </summary>
    /// <param name="handler">이벤트 핸들러</param>
    /// <param name="threadOption">실행 스레드 옵션</param>
    /// <param name="keepSubscriberReferenceAlive">
    ///     true이면 Strong Reference 유지 (기본값),
    ///     false이면 Weak Reference 사용 (구독자가 GC되면 자동 구독 해제)
    /// </param>
    SubscriptionToken Subscribe<TEvent>(Action<TEvent> handler, ThreadOption threadOption, bool keepSubscriberReferenceAlive) where TEvent : PubSubEvent;

    /// <summary>
    ///     비동기 이벤트 구독 (기본: UI 스레드)
    /// </summary>
    SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : PubSubEvent;

    /// <summary>
    ///     비동기 이벤트 구독 (스레드 옵션 지정)
    /// </summary>
    SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler, ThreadOption threadOption) where TEvent : PubSubEvent;

    /// <summary>
    ///     비동기 이벤트 구독 (스레드 옵션 및 참조 유지 옵션 지정)
    /// </summary>
    SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler, ThreadOption threadOption, bool keepSubscriberReferenceAlive) where TEvent : PubSubEvent;

    /// <summary>
    ///     이벤트 구독 (필터 조건 지정)
    ///     필터 조건을 만족하는 이벤트만 핸들러로 전달됩니다.
    /// </summary>
    /// <param name="handler">이벤트 핸들러</param>
    /// <param name="filter">이벤트 필터 (true 반환 시에만 핸들러 호출)</param>
    /// <param name="threadOption">실행 스레드 옵션</param>
    /// <param name="keepSubscriberReferenceAlive">참조 유지 옵션</param>
    SubscriptionToken Subscribe<TEvent>(
        Action<TEvent> handler,
        Predicate<TEvent> filter,
        ThreadOption threadOption = ThreadOption.UIThread,
        bool keepSubscriberReferenceAlive = true) where TEvent : PubSubEvent;

    /// <summary>
    ///     비동기 이벤트 구독 (필터 조건 지정)
    /// </summary>
    SubscriptionToken Subscribe<TEvent>(
        Func<TEvent, Task> handler,
        Predicate<TEvent> filter,
        ThreadOption threadOption = ThreadOption.UIThread,
        bool keepSubscriberReferenceAlive = true) where TEvent : PubSubEvent;

    /// <summary>
    ///     이벤트를 비동기로 발행
    /// </summary>
    Task PublishAsync<TEvent>(TEvent eventData) where TEvent : PubSubEvent;

    /// <summary>
    ///     이벤트를 발행
    /// </summary>
    /// <typeparam name="TEvent">이벤트 타입 (PubSubEvent 상속 필수)</typeparam>
    /// <param name="eventData">이벤트 데이터</param>
    void Publish<TEvent>(TEvent eventData) where TEvent : PubSubEvent;

    /// <summary>
    ///     이벤트 구독 해제
    /// </summary>
    void Unsubscribe(SubscriptionToken token);
}

/// <summary>
///     구독 해제를 위한 토큰
/// </summary>
public sealed class SubscriptionToken(Type eventType, Action<SubscriptionToken> unsubscribeAction)
    : IDisposable
{
    private bool _disposed;

    public Guid Id { get; } = Guid.NewGuid();
    public Type EventType { get; } = eventType;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        unsubscribeAction(this);
    }
}
