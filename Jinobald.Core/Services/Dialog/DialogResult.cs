namespace Jinobald.Core.Services.Dialog;

/// <summary>
///     다이얼로그 결과 구현
/// </summary>
public class DialogResult : IDialogResult
{
    /// <inheritdoc />
    public ButtonResult Result { get; }

    /// <inheritdoc />
    public IDialogParameters Parameters { get; }

    /// <summary>
    ///     DialogResult 생성자
    /// </summary>
    public DialogResult() : this(ButtonResult.None, new DialogParameters())
    {
    }

    /// <summary>
    ///     DialogResult 생성자
    /// </summary>
    /// <param name="result">버튼 결과</param>
    public DialogResult(ButtonResult result) : this(result, new DialogParameters())
    {
    }

    /// <summary>
    ///     DialogResult 생성자
    /// </summary>
    /// <param name="result">버튼 결과</param>
    /// <param name="parameters">다이얼로그 파라미터</param>
    public DialogResult(ButtonResult result, IDialogParameters parameters)
    {
        Result = result;
        Parameters = parameters ?? new DialogParameters();
    }

    /// <summary>
    ///     성공적으로 완료된 결과 (OK)
    /// </summary>
    public static DialogResult Ok() => new(ButtonResult.OK);

    /// <summary>
    ///     성공적으로 완료된 결과 (OK) with parameters
    /// </summary>
    public static DialogResult Ok(IDialogParameters parameters) => new(ButtonResult.OK, parameters);

    /// <summary>
    ///     취소된 결과 (Cancel)
    /// </summary>
    public static DialogResult Cancel() => new(ButtonResult.Cancel);

    /// <summary>
    ///     예 버튼 결과 (Yes)
    /// </summary>
    public static DialogResult Yes() => new(ButtonResult.Yes);

    /// <summary>
    ///     아니오 버튼 결과 (No)
    /// </summary>
    public static DialogResult No() => new(ButtonResult.No);
}

/// <summary>
///     제네릭 다이얼로그 결과 구현
///     강타입 데이터를 반환할 때 사용합니다.
/// </summary>
/// <typeparam name="T">반환 데이터 타입</typeparam>
public class DialogResult<T> : IDialogResult<T>
{
    /// <inheritdoc />
    public ButtonResult Result { get; }

    /// <inheritdoc />
    public IDialogParameters Parameters { get; }

    /// <inheritdoc />
    public T? Data { get; }

    /// <summary>
    ///     DialogResult 생성자
    /// </summary>
    public DialogResult() : this(ButtonResult.None, default, new DialogParameters())
    {
    }

    /// <summary>
    ///     DialogResult 생성자
    /// </summary>
    /// <param name="result">버튼 결과</param>
    /// <param name="data">반환 데이터</param>
    public DialogResult(ButtonResult result, T? data) : this(result, data, new DialogParameters())
    {
    }

    /// <summary>
    ///     DialogResult 생성자
    /// </summary>
    /// <param name="result">버튼 결과</param>
    /// <param name="data">반환 데이터</param>
    /// <param name="parameters">다이얼로그 파라미터</param>
    public DialogResult(ButtonResult result, T? data, IDialogParameters parameters)
    {
        Result = result;
        Data = data;
        Parameters = parameters ?? new DialogParameters();
    }

    /// <summary>
    ///     성공적으로 완료된 결과 (OK)
    /// </summary>
    /// <param name="data">반환 데이터</param>
    public static DialogResult<T> Ok(T data) => new(ButtonResult.OK, data);

    /// <summary>
    ///     성공적으로 완료된 결과 (OK) with parameters
    /// </summary>
    public static DialogResult<T> Ok(T data, IDialogParameters parameters) => new(ButtonResult.OK, data, parameters);

    /// <summary>
    ///     취소된 결과 (Cancel)
    /// </summary>
    public static DialogResult<T> Cancel() => new(ButtonResult.Cancel, default);

    /// <summary>
    ///     예 버튼 결과 (Yes)
    /// </summary>
    /// <param name="data">반환 데이터</param>
    public static DialogResult<T> Yes(T data) => new(ButtonResult.Yes, data);

    /// <summary>
    ///     아니오 버튼 결과 (No)
    /// </summary>
    public static DialogResult<T> No() => new(ButtonResult.No, default);

    /// <summary>
    ///     DialogResult에서 변환
    /// </summary>
    public static implicit operator DialogResult<T>(DialogResult result)
    {
        return new DialogResult<T>(result.Result, default, result.Parameters);
    }
}

/// <summary>
///     IDialogResult 확장 메서드
/// </summary>
public static class DialogResultExtensions
{
    /// <summary>
    ///     결과가 성공(OK, Yes)인지 확인
    /// </summary>
    public static bool IsSuccess(this IDialogResult result)
    {
        return result.Result is ButtonResult.OK or ButtonResult.Yes;
    }

    /// <summary>
    ///     결과가 취소(Cancel, No)인지 확인
    /// </summary>
    public static bool IsCancelled(this IDialogResult result)
    {
        return result.Result is ButtonResult.Cancel or ButtonResult.No;
    }

    /// <summary>
    ///     IDialogResult를 IDialogResult{T}로 캐스팅
    /// </summary>
    public static IDialogResult<T>? AsTyped<T>(this IDialogResult result)
    {
        return result as IDialogResult<T>;
    }

    /// <summary>
    ///     데이터를 안전하게 가져오기
    /// </summary>
    public static T? GetData<T>(this IDialogResult result)
    {
        if (result is IDialogResult<T> typedResult)
            return typedResult.Data;

        return default;
    }

    /// <summary>
    ///     성공 시 데이터를 가져오고, 실패 시 기본값 반환
    /// </summary>
    public static T? GetDataOrDefault<T>(this IDialogResult result, T? defaultValue = default)
    {
        if (result.IsSuccess() && result is IDialogResult<T> typedResult)
            return typedResult.Data;

        return defaultValue;
    }
}
