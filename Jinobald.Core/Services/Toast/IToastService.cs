using System.Collections.ObjectModel;

namespace Jinobald.Core.Services.Toast;

/// <summary>
///     토스트 알림 서비스
///     비침투적(non-intrusive) 알림을 표시합니다.
///     여러 토스트를 동시에 표시할 수 있으며, 자동으로 닫힙니다.
/// </summary>
public interface IToastService
{
    /// <summary>
    ///     토스트 호스트를 등록합니다.
    ///     MainWindow 또는 Shell에서 호출해야 합니다.
    /// </summary>
    /// <param name="host">토스트 호스트</param>
    void RegisterHost(IToastHost host);

    /// <summary>
    ///     성공 메시지 토스트를 표시합니다.
    /// </summary>
    /// <param name="message">메시지 내용</param>
    /// <param name="title">제목 (선택)</param>
    /// <param name="duration">표시 시간(초). null이면 기본값 사용</param>
    void ShowSuccess(string message, string? title = null, int? duration = null);

    /// <summary>
    ///     정보 메시지 토스트를 표시합니다.
    /// </summary>
    /// <param name="message">메시지 내용</param>
    /// <param name="title">제목 (선택)</param>
    /// <param name="duration">표시 시간(초). null이면 기본값 사용</param>
    void ShowInfo(string message, string? title = null, int? duration = null);

    /// <summary>
    ///     경고 메시지 토스트를 표시합니다.
    /// </summary>
    /// <param name="message">메시지 내용</param>
    /// <param name="title">제목 (선택)</param>
    /// <param name="duration">표시 시간(초). null이면 기본값 사용</param>
    void ShowWarning(string message, string? title = null, int? duration = null);

    /// <summary>
    ///     에러 메시지 토스트를 표시합니다.
    /// </summary>
    /// <param name="message">메시지 내용</param>
    /// <param name="title">제목 (선택)</param>
    /// <param name="duration">표시 시간(초). null이면 기본값 사용</param>
    void ShowError(string message, string? title = null, int? duration = null);

    /// <summary>
    ///     커스텀 토스트를 표시합니다.
    /// </summary>
    /// <param name="toast">토스트 메시지</param>
    void Show(ToastMessage toast);

    /// <summary>
    ///     모든 토스트를 닫습니다.
    /// </summary>
    void ClearAll();
}

/// <summary>
///     토스트 메시지
/// </summary>
public class ToastMessage
{
    /// <summary>
    ///     고유 ID
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     제목
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     메시지 내용
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    ///     토스트 타입
    /// </summary>
    public ToastType Type { get; init; } = ToastType.Info;

    /// <summary>
    ///     표시 시간(초). 0이면 자동으로 닫히지 않음
    /// </summary>
    public int Duration { get; init; } = 3;

    /// <summary>
    ///     생성 시간
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}

/// <summary>
///     토스트 타입
/// </summary>
public enum ToastType
{
    /// <summary>정보</summary>
    Info,

    /// <summary>성공</summary>
    Success,

    /// <summary>경고</summary>
    Warning,

    /// <summary>에러</summary>
    Error
}

/// <summary>
///     토스트 표시 위치
/// </summary>
public enum ToastPosition
{
    /// <summary>상단 오른쪽</summary>
    TopRight,

    /// <summary>상단 왼쪽</summary>
    TopLeft,

    /// <summary>상단 중앙</summary>
    TopCenter,

    /// <summary>하단 오른쪽</summary>
    BottomRight,

    /// <summary>하단 왼쪽</summary>
    BottomLeft,

    /// <summary>하단 중앙</summary>
    BottomCenter
}

/// <summary>
///     토스트 호스트 인터페이스
///     ToastService가 토스트를 표시할 컨테이너
/// </summary>
public interface IToastHost
{
    /// <summary>
    ///     활성 토스트 컬렉션
    ///     새로운 토스트가 추가되면 표시됩니다.
    /// </summary>
    ObservableCollection<ToastMessage> Toasts { get; }

    /// <summary>
    ///     토스트 표시 위치
    /// </summary>
    ToastPosition Position { get; set; }

    /// <summary>
    ///     최대 토스트 개수
    /// </summary>
    int MaxToasts { get; set; }
}
