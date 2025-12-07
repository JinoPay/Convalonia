using System.Windows.Input;

namespace Jinobald.Core.Commands;

/// <summary>
///     여러 명령을 하나로 조합하는 복합 명령
///     Prism의 CompositeCommand와 유사한 구현
/// </summary>
/// <remarks>
///     - 등록된 모든 명령의 CanExecute가 true일 때만 실행 가능
///     - 모든 등록된 명령을 순차적으로 실행
///     - 개별 명령의 CanExecuteChanged 이벤트를 자동으로 전파
/// </remarks>
public class CompositeCommand : ICommand
{
    private readonly List<ICommand> _registeredCommands = new();
    private readonly object _lock = new();
    private readonly bool _monitorCommandActivity;

    /// <summary>
    ///     CompositeCommand 생성자
    /// </summary>
    public CompositeCommand() : this(false)
    {
    }

    /// <summary>
    ///     CompositeCommand 생성자
    /// </summary>
    /// <param name="monitorCommandActivity">
    ///     true이면 활성 명령만 고려 (IActiveAware 구현체)
    /// </param>
    public CompositeCommand(bool monitorCommandActivity)
    {
        _monitorCommandActivity = monitorCommandActivity;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    ///     등록된 명령 목록
    /// </summary>
    public IReadOnlyList<ICommand> RegisteredCommands
    {
        get
        {
            lock (_lock)
            {
                return _registeredCommands.ToList();
            }
        }
    }

    /// <summary>
    ///     등록된 명령 수
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _registeredCommands.Count;
            }
        }
    }

    /// <summary>
    ///     명령을 등록합니다.
    /// </summary>
    /// <param name="command">등록할 명령</param>
    /// <exception cref="ArgumentNullException">command가 null인 경우</exception>
    /// <exception cref="InvalidOperationException">이미 등록된 명령인 경우</exception>
    public virtual void RegisterCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        lock (_lock)
        {
            if (_registeredCommands.Contains(command))
                throw new InvalidOperationException("Command is already registered.");

            _registeredCommands.Add(command);
        }

        command.CanExecuteChanged += OnRegisteredCommandCanExecuteChanged;
        RaiseCanExecuteChanged();
    }

    /// <summary>
    ///     명령을 해제합니다.
    /// </summary>
    /// <param name="command">해제할 명령</param>
    /// <exception cref="ArgumentNullException">command가 null인 경우</exception>
    public virtual void UnregisterCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool removed;
        lock (_lock)
        {
            removed = _registeredCommands.Remove(command);
        }

        if (removed)
        {
            command.CanExecuteChanged -= OnRegisteredCommandCanExecuteChanged;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    ///     등록된 모든 명령을 해제합니다.
    /// </summary>
    public virtual void ClearCommands()
    {
        ICommand[] commandsCopy;
        lock (_lock)
        {
            commandsCopy = _registeredCommands.ToArray();
            _registeredCommands.Clear();
        }

        foreach (var command in commandsCopy)
        {
            command.CanExecuteChanged -= OnRegisteredCommandCanExecuteChanged;
        }

        RaiseCanExecuteChanged();
    }

    /// <inheritdoc />
    public virtual bool CanExecute(object? parameter)
    {
        ICommand[] commands;
        lock (_lock)
        {
            commands = _registeredCommands.ToArray();
        }

        if (commands.Length == 0)
            return false;

        foreach (var command in commands)
        {
            if (!ShouldExecute(command))
                continue;

            if (!command.CanExecute(parameter))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public virtual void Execute(object? parameter)
    {
        ICommand[] commands;
        lock (_lock)
        {
            commands = _registeredCommands.ToArray();
        }

        foreach (var command in commands)
        {
            if (ShouldExecute(command) && command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }
    }

    /// <summary>
    ///     CanExecuteChanged 이벤트를 발생시킵니다.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     명령이 실행되어야 하는지 확인합니다.
    ///     monitorCommandActivity가 true이면 IActiveAware.IsActive를 확인합니다.
    /// </summary>
    /// <param name="command">확인할 명령</param>
    /// <returns>실행 여부</returns>
    protected virtual bool ShouldExecute(ICommand command)
    {
        if (!_monitorCommandActivity)
            return true;

        // IActiveAware를 구현하는 경우 IsActive 확인
        if (command is IActiveAware activeAware)
            return activeAware.IsActive;

        return true;
    }

    private void OnRegisteredCommandCanExecuteChanged(object? sender, EventArgs e)
    {
        RaiseCanExecuteChanged();
    }
}

/// <summary>
///     활성 상태를 나타내는 인터페이스
/// </summary>
public interface IActiveAware
{
    /// <summary>
    ///     활성 상태 여부
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    ///     활성 상태가 변경될 때 발생하는 이벤트
    /// </summary>
    event EventHandler? IsActiveChanged;
}
