using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace Jinobald.Core.Mvvm;

/// <summary>
///     검증 기능을 제공하는 ViewModel 기본 클래스
///     CommunityToolkit.Mvvm의 ObservableValidator를 상속하고
///     INotifyDataErrorInfo, DataAnnotations 기반 검증을 지원합니다.
/// </summary>
/// <remarks>
///     사용 예:
///     <code>
/// public partial class UserViewModel : ValidatableViewModelBase
/// {
///     [ObservableProperty]
///     [NotifyDataErrorInfo]
///     [Required(ErrorMessage = "이름은 필수입니다")]
///     [MinLength(2, ErrorMessage = "이름은 2자 이상이어야 합니다")]
///     private string _name = string.Empty;
///
///     [ObservableProperty]
///     [NotifyDataErrorInfo]
///     [EmailAddress(ErrorMessage = "올바른 이메일 형식이 아닙니다")]
///     private string _email = string.Empty;
///
///     [RelayCommand(CanExecute = nameof(CanSave))]
///     private void Save() { /* ... */ }
///
///     private bool CanSave() => !HasErrors;
/// }
/// </code>
/// </remarks>
public abstract class ValidatableViewModelBase : ObservableValidator,
    IInitializableAsync,
    IActivatable,
    IDestructible,
    IDisposable
{
    private bool _disposed;
    private readonly Dictionary<string, List<ValidationResult>> _customErrors = new();

    /// <summary>
    ///     로거 인스턴스
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    ///     ViewModel이 초기화되었는지 여부
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///     ViewModel이 활성화되었는지 여부
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    ///     리소스가 해제되었는지 여부
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    ///     검증 오류가 있는지 여부 (DataAnnotation + Custom Errors)
    /// </summary>
    public new bool HasErrors => base.HasErrors || _customErrors.Values.Any(e => e.Count > 0);

    protected ValidatableViewModelBase()
    {
        Logger = Log.ForContext(GetType());
    }

    #region Custom Validation Methods

    /// <summary>
    ///     특정 속성에 커스텀 검증 오류 추가
    /// </summary>
    /// <param name="propertyName">속성 이름</param>
    /// <param name="errorMessage">오류 메시지</param>
    protected void AddError(string propertyName, string errorMessage)
    {
        if (!_customErrors.ContainsKey(propertyName))
            _customErrors[propertyName] = new List<ValidationResult>();

        var error = new ValidationResult(errorMessage, new[] { propertyName });
        if (_customErrors[propertyName].All(e => e.ErrorMessage != errorMessage))
        {
            _customErrors[propertyName].Add(error);
            OnErrorsChanged(propertyName);
        }
    }

    /// <summary>
    ///     특정 속성의 커스텀 검증 오류 제거
    /// </summary>
    /// <param name="propertyName">속성 이름</param>
    protected void ClearCustomErrors(string propertyName)
    {
        if (_customErrors.ContainsKey(propertyName) && _customErrors[propertyName].Count > 0)
        {
            _customErrors[propertyName].Clear();
            OnErrorsChanged(propertyName);
        }
    }

    /// <summary>
    ///     모든 커스텀 검증 오류 제거
    /// </summary>
    protected void ClearAllCustomErrors()
    {
        var propertiesToClear = _customErrors.Keys.Where(k => _customErrors[k].Count > 0).ToList();
        _customErrors.Clear();
        foreach (var propertyName in propertiesToClear)
        {
            OnErrorsChanged(propertyName);
        }
    }

    /// <summary>
    ///     ErrorsChanged 이벤트 발생
    /// </summary>
    protected void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }

    /// <summary>
    ///     특정 속성의 모든 오류 가져오기 (DataAnnotation + Custom)
    /// </summary>
    public new IEnumerable GetErrors(string? propertyName)
    {
        var dataAnnotationErrors = base.GetErrors(propertyName);

        if (string.IsNullOrEmpty(propertyName))
        {
            // 모든 오류 반환
            foreach (var error in dataAnnotationErrors)
                yield return error;

            foreach (var errors in _customErrors.Values)
                foreach (var error in errors)
                    yield return error;
        }
        else
        {
            // 특정 속성 오류만 반환
            foreach (var error in dataAnnotationErrors)
                yield return error;

            if (_customErrors.TryGetValue(propertyName, out var customErrors))
                foreach (var error in customErrors)
                    yield return error;
        }
    }

    /// <summary>
    ///     특정 속성의 오류 메시지 목록 가져오기
    /// </summary>
    public IReadOnlyList<string> GetErrorMessages(string propertyName)
    {
        var messages = new List<string>();

        foreach (var error in GetErrors(propertyName))
        {
            if (error is ValidationResult validationResult && !string.IsNullOrEmpty(validationResult.ErrorMessage))
                messages.Add(validationResult.ErrorMessage);
            else if (error is string errorString)
                messages.Add(errorString);
        }

        return messages;
    }

    /// <summary>
    ///     모든 오류 메시지 가져오기
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetAllErrorMessages()
    {
        var allErrors = new Dictionary<string, IReadOnlyList<string>>();

        // 모든 속성에 대해 오류 수집
        foreach (var propertyName in GetPropertiesWithErrors())
        {
            var messages = GetErrorMessages(propertyName);
            if (messages.Count > 0)
                allErrors[propertyName] = messages;
        }

        return allErrors;
    }

    /// <summary>
    ///     오류가 있는 속성 이름 목록
    /// </summary>
    private IEnumerable<string> GetPropertiesWithErrors()
    {
        // DataAnnotation 오류가 있는 속성들
        foreach (var prop in GetType().GetProperties())
        {
            if (base.GetErrors(prop.Name).Cast<object>().Any())
                yield return prop.Name;
        }

        // Custom 오류가 있는 속성들
        foreach (var prop in _customErrors.Keys)
        {
            if (_customErrors[prop].Count > 0)
                yield return prop;
        }
    }

    /// <summary>
    ///     모든 속성 검증 수행
    /// </summary>
    public new void ValidateAllProperties()
    {
        base.ValidateAllProperties();
    }

    /// <summary>
    ///     특정 속성 검증 수행
    /// </summary>
    /// <param name="propertyName">검증할 속성 이름</param>
    public void ValidateProperty([CallerMemberName] string? propertyName = null)
    {
        if (!string.IsNullOrEmpty(propertyName))
        {
            var propertyInfo = GetType().GetProperty(propertyName);
            if (propertyInfo != null)
            {
                var value = propertyInfo.GetValue(this);
                ValidateProperty(value, propertyName);
            }
        }
    }

    /// <summary>
    ///     ErrorsChanged 이벤트
    /// </summary>
    public new event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    #endregion

    #region IInitializableAsync 구현

    async Task IInitializableAsync.InitializeAsync(CancellationToken cancellationToken)
    {
        if (IsInitialized)
            return;

        await OnInitializeAsync(cancellationToken);
        IsInitialized = true;
        Logger.Debug("{ViewModelName} 초기화됨", GetType().Name);
    }

    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region IActivatable 구현

    async Task IActivatable.ActivateAsync(CancellationToken cancellationToken)
    {
        await OnActivateAsync(cancellationToken);
        IsActive = true;
        Logger.Debug("{ViewModelName} 활성화됨", GetType().Name);
    }

    async Task IActivatable.DeactivateAsync(CancellationToken cancellationToken)
    {
        await OnDeactivateAsync(cancellationToken);
        IsActive = false;
        Logger.Debug("{ViewModelName} 비활성화됨", GetType().Name);
    }

    protected virtual Task OnActivateAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDeactivateAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region IDestructible 구현

    public void Destroy()
    {
        Dispose(true);
    }

    protected virtual void OnDestroy(bool disposing)
    {
    }

    #endregion

    #region IDisposable 구현

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            OnDestroy(disposing);
            ClearAllCustomErrors();
            Logger.Debug("{ViewModelName} Disposed", GetType().Name);
        }

        _disposed = true;
    }

    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    #endregion
}
