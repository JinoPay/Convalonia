using Avalonia.Controls;
using Avalonia.Threading;
using Jinobald.Avalonia.Mvvm;
using Jinobald.Core.Ioc;
using Jinobald.Core.Services.Dialog;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Jinobald.Avalonia.Services.Dialog;

/// <summary>
///     Avalonia 다이얼로그 서비스 구현
///     in-window overlay 방식으로 다이얼로그를 표시합니다.
///     중첩 다이얼로그를 지원합니다.
///     View-First 방식으로 동작합니다.
/// </summary>
public class DialogService : IDialogService
{
    private readonly ILogger _logger;
    private IDialogHost? _dialogHost;
    private readonly Stack<DialogContext> _dialogStack = new();

    public DialogService()
    {
        _logger = Log.ForContext<DialogService>();
    }

    /// <summary>
    ///     다이얼로그 호스트를 등록합니다.
    ///     MainWindow 또는 Shell에서 호출해야 합니다.
    /// </summary>
    public void RegisterHost(IDialogHost host)
    {
        _dialogHost = host ?? throw new ArgumentNullException(nameof(host));
        _logger.Information("다이얼로그 호스트 등록 완료");
    }

    /// <inheritdoc />
    public Task<IDialogResult?> ShowDialogAsync<TView>(IDialogParameters? parameters = null)
    {
        return ShowDialogAsync(typeof(TView), parameters);
    }

    /// <inheritdoc />
    public async Task<IDialogResult?> ShowDialogAsync(Type viewType, IDialogParameters? parameters = null)
    {
        _logger.Debug("다이얼로그 표시: {ViewType}", viewType.Name);

        // DialogHost 확인
        if (_dialogHost == null)
        {
            _logger.Error("다이얼로그 호스트가 등록되지 않음");
            throw new InvalidOperationException("다이얼로그 호스트가 등록되지 않았습니다. RegisterHost()를 먼저 호출하세요.");
        }

        // View 생성 (DI 우선, 없으면 ActivatorUtilities로 생성)
        var view = ResolveOrCreate(viewType);
        if (view == null)
        {
            _logger.Error("View를 생성할 수 없습니다: {ViewType}", viewType.Name);
            return null;
        }

        // ViewModel 타입 추론 (View-First)
        var viewModelType = ViewModelLocator.ResolveViewModelType(viewType);
        if (viewModelType == null)
        {
            _logger.Error("ViewModel 타입을 찾을 수 없습니다: {ViewType}", viewType.Name);
            return null;
        }

        // ViewModel 생성 (DI 우선, 없으면 ActivatorUtilities로 생성)
        var viewModel = ResolveOrCreate(viewModelType) as IDialogAware;
        if (viewModel == null)
        {
            _logger.Error("ViewModel을 생성할 수 없거나 IDialogAware를 구현하지 않았습니다: {ViewModelType}", viewModelType.Name);
            return null;
        }

        return await ShowDialogInternalAsync(view, viewModel, parameters);
    }

    private async Task<IDialogResult?> ShowDialogInternalAsync(object view, IDialogAware viewModel, IDialogParameters? parameters)
    {
        if (_dialogHost == null)
        {
            _logger.Error("다이얼로그 호스트가 등록되지 않음");
            throw new InvalidOperationException("다이얼로그 호스트가 등록되지 않았습니다.");
        }

        // DialogContext 생성 (중첩 지원)
        var context = new DialogContext
        {
            View = view,
            ViewModel = viewModel,
            TaskCompletionSource = new TaskCompletionSource<IDialogResult?>()
        };

        // 스택에 추가
        _dialogStack.Push(context);
        _logger.Debug("다이얼로그 스택 깊이: {Depth}", _dialogStack.Count);

        // ViewModel에 이벤트 연결
        viewModel.RequestClose += OnRequestClose;

        // ViewModel의 OnDialogOpened 호출
        viewModel.OnDialogOpened(parameters ?? new DialogParameters());

        // UI 쓰레드에서 다이얼로그 표시
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (view is Control control)
            {
                control.DataContext = viewModel;
            }

            // DialogHost의 스택에 추가 (가장 최근 다이얼로그가 맨 위에 표시됨)
            _dialogHost.DialogStack.Add(view);
        });

        // 다이얼로그가 닫힐 때까지 대기
        var result = await context.TaskCompletionSource.Task;
        return result;
    }

    private void OnRequestClose(IDialogResult result)
    {
        // 비동기 처리를 위해 Task.Run 사용
        _ = OnRequestCloseAsync(result);
    }

    private async Task OnRequestCloseAsync(IDialogResult result)
    {
        if (_dialogHost == null || _dialogStack.Count == 0)
            return;

        // 가장 최근 다이얼로그 가져오기
        var context = _dialogStack.Peek();
        var viewModel = context.ViewModel;

        try
        {
            // ViewModel의 CanCloseDialogAsync 확인 (비동기)
            var canClose = await viewModel.CanCloseDialogAsync();
            if (!canClose)
            {
                _logger.Debug("다이얼로그 닫기가 취소됨");
                return;
            }

            // OnClosingAsync 호출 (닫히기 직전)
            await viewModel.OnClosingAsync();

            // 스택에서 제거
            _dialogStack.Pop();
            _logger.Debug("다이얼로그 스택 깊이: {Depth}", _dialogStack.Count);

            // 다이얼로그 닫기 (UI 스레드)
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // OnDialogClosed 호출 (완전히 닫힌 후)
                viewModel.OnDialogClosed();

                // 이벤트 해제
                viewModel.RequestClose -= OnRequestClose;

                // DialogHost 스택에서 제거
                if (_dialogHost.DialogStack.Contains(context.View))
                {
                    _dialogHost.DialogStack.Remove(context.View);
                }

                // TaskCompletionSource 완료
                context.TaskCompletionSource.TrySetResult(result);
            });

            _logger.Debug("다이얼로그 닫힘");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "다이얼로그 닫기 중 오류 발생");
            // 오류 발생 시에도 TaskCompletionSource 완료
            context.TaskCompletionSource.TrySetException(ex);
        }
    }

    /// <summary>
    ///     DI 컨테이너에서 먼저 resolve를 시도하고, 등록되지 않은 경우 ActivatorUtilities로 생성합니다.
    /// </summary>
    private static object? ResolveOrCreate(Type type)
    {
        var serviceProvider = (IServiceProvider)ContainerLocator.Current.Instance;

        // DI에서 먼저 시도
        var service = serviceProvider.GetService(type);
        if (service != null)
            return service;

        // DI에 없으면 ActivatorUtilities로 생성 (생성자 의존성 주입 지원)
        return ActivatorUtilities.CreateInstance(serviceProvider, type);
    }

    /// <summary>
    ///     다이얼로그 컨텍스트 (중첩 지원용)
    /// </summary>
    private class DialogContext
    {
        public required object View { get; init; }
        public required IDialogAware ViewModel { get; init; }
        public required TaskCompletionSource<IDialogResult?> TaskCompletionSource { get; init; }
    }
}
