using Jinobald.Core.Services.Dialog;

namespace Jinobald.Core.Mvvm;

/// <summary>
///     모든 다이얼로그 ViewModel의 기본 클래스 (Prism 스타일)
///     ButtonResult와 IDialogParameters를 사용하여 결과를 전달합니다.
/// </summary>
public abstract class DialogViewModelBase : ViewModelBase, IDialogAware
{
    #region IDialogAware 구현

    /// <summary>
    ///     다이얼로그 닫기 요청 이벤트
    /// </summary>
    public event Action<IDialogResult>? RequestClose;

    /// <summary>
    ///     다이얼로그가 열렸을 때 호출됩니다.
    ///     파생 클래스에서 파라미터를 처리하려면 이 메서드를 오버라이드하세요.
    /// </summary>
    /// <param name="parameters">전달된 파라미터</param>
    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
    }

    /// <summary>
    ///     다이얼로그를 닫을 수 있는지 비동기로 확인합니다.
    ///     파생 클래스에서 닫기 검증 로직을 추가하려면 이 메서드를 오버라이드하세요.
    ///     예: "정말로 닫으시겠습니까?" 같은 확인 다이얼로그 표시
    /// </summary>
    /// <returns>닫기 가능 여부 (true: 닫기 허용, false: 닫기 취소)</returns>
    public virtual Task<bool> CanCloseDialogAsync()
    {
        return Task.FromResult(true);
    }

    /// <summary>
    ///     다이얼로그가 닫히기 직전에 호출됩니다.
    ///     파생 클래스에서 정리 작업이나 저장 로직을 추가하려면 이 메서드를 오버라이드하세요.
    /// </summary>
    public virtual Task OnClosingAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     다이얼로그가 완전히 닫힌 후 호출됩니다.
    ///     파생 클래스에서 최종 정리 작업이 필요하면 이 메서드를 오버라이드하세요.
    /// </summary>
    public virtual void OnDialogClosed()
    {
    }

    #endregion

    /// <summary>
    ///     다이얼로그를 닫습니다. (ButtonResult.None)
    /// </summary>
    protected void Close()
    {
        CloseWithButtonResult(ButtonResult.None);
    }

    /// <summary>
    ///     버튼 결과와 함께 다이얼로그를 닫습니다.
    ///     실제 닫기 검증은 DialogService의 CanCloseDialogAsync()에서 수행됩니다.
    /// </summary>
    /// <param name="buttonResult">버튼 결과 (OK, Cancel, Yes, No 등)</param>
    protected void CloseWithButtonResult(ButtonResult buttonResult)
    {
        var dialogResult = new DialogResult(buttonResult);
        RequestClose?.Invoke(dialogResult);
    }

    /// <summary>
    ///     버튼 결과와 커스텀 파라미터와 함께 다이얼로그를 닫습니다.
    ///     실제 닫기 검증은 DialogService의 CanCloseDialogAsync()에서 수행됩니다.
    /// </summary>
    /// <param name="buttonResult">버튼 결과</param>
    /// <param name="parameters">다이얼로그 결과 파라미터 (커스텀 데이터 전달용)</param>
    protected void CloseWithParameters(ButtonResult buttonResult, IDialogParameters parameters)
    {
        var dialogResult = new DialogResult(buttonResult, parameters);
        RequestClose?.Invoke(dialogResult);
    }

    /// <summary>
    ///     커스텀 파라미터와 함께 다이얼로그를 닫습니다. (ButtonResult.None 사용)
    /// </summary>
    /// <param name="parameters">다이얼로그 결과 파라미터</param>
    protected void CloseWithParameters(IDialogParameters parameters)
    {
        CloseWithParameters(ButtonResult.None, parameters);
    }
}
