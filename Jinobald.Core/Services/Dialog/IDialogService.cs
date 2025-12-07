using System.Collections.ObjectModel;

namespace Jinobald.Core.Services.Dialog;

/// <summary>
///     다이얼로그 표시 서비스
///     오버레이 기반의 in-window 다이얼로그를 표시합니다.
///     중첩 다이얼로그를 지원합니다.
///     View-First 방식으로 동작합니다.
/// </summary>
public interface IDialogService
{
    /// <summary>
    ///     다이얼로그 호스트를 등록합니다.
    ///     MainWindow 또는 Shell에서 호출해야 합니다.
    /// </summary>
    /// <param name="host">다이얼로그 호스트</param>
    void RegisterHost(IDialogHost host);

    /// <summary>
    ///     다이얼로그를 표시합니다. (View-First)
    ///     ViewModel은 ViewModelLocator를 통해 자동으로 resolve됩니다.
    ///     중첩 다이얼로그를 지원합니다.
    /// </summary>
    /// <typeparam name="TView">다이얼로그 View 타입</typeparam>
    /// <param name="parameters">다이얼로그에 전달할 파라미터</param>
    /// <returns>다이얼로그 결과</returns>
    Task<IDialogResult?> ShowDialogAsync<TView>(IDialogParameters? parameters = null);

    /// <summary>
    ///     다이얼로그를 표시합니다. (View 타입으로)
    ///     중첩 다이얼로그를 지원합니다.
    /// </summary>
    /// <param name="viewType">다이얼로그 View 타입</param>
    /// <param name="parameters">다이얼로그에 전달할 파라미터</param>
    /// <returns>다이얼로그 결과</returns>
    Task<IDialogResult?> ShowDialogAsync(Type viewType, IDialogParameters? parameters = null);
}

/// <summary>
///     다이얼로그 ViewModel 인터페이스
///     비동기 라이프사이클을 지원합니다.
/// </summary>
public interface IDialogAware
{
    /// <summary>
    ///     다이얼로그가 열렸을 때 호출됩니다.
    /// </summary>
    /// <param name="parameters">전달된 파라미터</param>
    void OnDialogOpened(IDialogParameters parameters);

    /// <summary>
    ///     다이얼로그를 닫을 수 있는지 비동기로 확인합니다.
    ///     예: "정말로 닫으시겠습니까?" 같은 확인 다이얼로그 표시 가능
    /// </summary>
    /// <returns>닫기 가능 여부 (true: 닫기 허용, false: 닫기 취소)</returns>
    Task<bool> CanCloseDialogAsync();

    /// <summary>
    ///     다이얼로그가 닫히기 직전에 호출됩니다.
    ///     정리 작업, 저장 확인 등을 수행할 수 있습니다.
    /// </summary>
    Task OnClosingAsync();

    /// <summary>
    ///     다이얼로그가 완전히 닫힌 후 호출됩니다.
    /// </summary>
    void OnDialogClosed();

    /// <summary>
    ///     다이얼로그 닫기 요청 이벤트
    /// </summary>
    event Action<IDialogResult>? RequestClose;
}

/// <summary>
///     제네릭 다이얼로그 ViewModel 인터페이스
///     강타입 데이터를 반환할 때 사용합니다.
/// </summary>
/// <typeparam name="T">반환 데이터 타입</typeparam>
public interface IDialogAware<T> : IDialogAware
{
    /// <summary>
    ///     다이얼로그 닫기 요청 이벤트 (강타입)
    /// </summary>
    new event Action<IDialogResult<T>>? RequestClose;
}

/// <summary>
///     다이얼로그 파라미터
/// </summary>
public interface IDialogParameters
{
    void Add(string key, object value);
    T? GetValue<T>(string key);
    bool ContainsKey(string key);
}

/// <summary>
///     다이얼로그 버튼 결과 (Prism 스타일)
/// </summary>
public enum ButtonResult
{
    /// <summary>결과 없음</summary>
    None = 0,

    /// <summary>OK 버튼</summary>
    OK = 1,

    /// <summary>Cancel 버튼</summary>
    Cancel = 2,

    /// <summary>Yes 버튼</summary>
    Yes = 3,

    /// <summary>No 버튼</summary>
    No = 4,

    /// <summary>Abort 버튼</summary>
    Abort = 5,

    /// <summary>Retry 버튼</summary>
    Retry = 6,

    /// <summary>Ignore 버튼</summary>
    Ignore = 7
}

/// <summary>
///     다이얼로그 결과
/// </summary>
public interface IDialogResult
{
    /// <summary>
    ///     버튼 결과 (OK, Cancel, Yes, No 등)
    /// </summary>
    ButtonResult Result { get; }

    /// <summary>
    ///     다이얼로그 파라미터 (추가 데이터 전달용)
    /// </summary>
    IDialogParameters Parameters { get; }
}

/// <summary>
///     제네릭 다이얼로그 결과
///     강타입 데이터를 반환할 때 사용합니다.
/// </summary>
/// <typeparam name="T">반환 데이터 타입</typeparam>
public interface IDialogResult<out T> : IDialogResult
{
    /// <summary>
    ///     다이얼로그에서 반환된 데이터
    /// </summary>
    T? Data { get; }
}

/// <summary>
///     다이얼로그 호스트 인터페이스
///     DialogService가 다이얼로그를 표시할 컨테이너
///     중첩 다이얼로그를 위한 스택 기반 구조를 제공합니다.
/// </summary>
public interface IDialogHost
{
    /// <summary>
    ///     활성 다이얼로그 스택 (중첩 지원)
    ///     가장 최근 다이얼로그가 맨 위에 표시됩니다.
    /// </summary>
    ObservableCollection<object> DialogStack { get; }

    /// <summary>
    ///     다이얼로그가 하나 이상 열려있는지 여부
    /// </summary>
    bool HasDialogs { get; }
}
