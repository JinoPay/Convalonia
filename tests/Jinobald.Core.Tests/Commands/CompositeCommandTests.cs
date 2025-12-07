using System.Windows.Input;
using Jinobald.Core.Commands;
using Xunit;

namespace Jinobald.Core.Tests.Commands;

public class CompositeCommandTests
{
    private class MockCommand : ICommand
    {
        public bool CanExecuteValue { get; set; } = true;
        public int ExecuteCount { get; private set; }
        public object? LastParameter { get; private set; }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => CanExecuteValue;

        public void Execute(object? parameter)
        {
            ExecuteCount++;
            LastParameter = parameter;
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private class ActiveAwareCommand : ICommand, IActiveAware
    {
        public bool CanExecuteValue { get; set; } = true;
        public int ExecuteCount { get; private set; }
        public bool IsActive { get; set; } = true;

        public event EventHandler? CanExecuteChanged;
        public event EventHandler? IsActiveChanged;

        public bool CanExecute(object? parameter) => CanExecuteValue;

        public void Execute(object? parameter) => ExecuteCount++;

        public void RaiseIsActiveChanged() => IsActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    [Fact]
    public void RegisterCommand_ShouldAddToRegisteredCommands()
    {
        // Arrange
        var composite = new CompositeCommand();
        var command = new MockCommand();

        // Act
        composite.RegisterCommand(command);

        // Assert
        Assert.Equal(1, composite.Count);
        Assert.Contains(command, composite.RegisteredCommands);
    }

    [Fact]
    public void RegisterCommand_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var composite = new CompositeCommand();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => composite.RegisterCommand(null!));
    }

    [Fact]
    public void RegisterCommand_Duplicate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var composite = new CompositeCommand();
        var command = new MockCommand();
        composite.RegisterCommand(command);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => composite.RegisterCommand(command));
    }

    [Fact]
    public void UnregisterCommand_ShouldRemoveFromRegisteredCommands()
    {
        // Arrange
        var composite = new CompositeCommand();
        var command = new MockCommand();
        composite.RegisterCommand(command);

        // Act
        composite.UnregisterCommand(command);

        // Assert
        Assert.Equal(0, composite.Count);
    }

    [Fact]
    public void UnregisterCommand_NotRegistered_ShouldNotThrow()
    {
        // Arrange
        var composite = new CompositeCommand();
        var command = new MockCommand();

        // Act & Assert - should not throw
        composite.UnregisterCommand(command);
    }

    [Fact]
    public void ClearCommands_ShouldRemoveAllCommands()
    {
        // Arrange
        var composite = new CompositeCommand();
        composite.RegisterCommand(new MockCommand());
        composite.RegisterCommand(new MockCommand());
        composite.RegisterCommand(new MockCommand());

        // Act
        composite.ClearCommands();

        // Assert
        Assert.Equal(0, composite.Count);
    }

    [Fact]
    public void CanExecute_NoCommands_ShouldReturnFalse()
    {
        // Arrange
        var composite = new CompositeCommand();

        // Act
        var result = composite.CanExecute(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanExecute_AllCommandsCanExecute_ShouldReturnTrue()
    {
        // Arrange
        var composite = new CompositeCommand();
        composite.RegisterCommand(new MockCommand { CanExecuteValue = true });
        composite.RegisterCommand(new MockCommand { CanExecuteValue = true });

        // Act
        var result = composite.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanExecute_OneCommandCannotExecute_ShouldReturnFalse()
    {
        // Arrange
        var composite = new CompositeCommand();
        composite.RegisterCommand(new MockCommand { CanExecuteValue = true });
        composite.RegisterCommand(new MockCommand { CanExecuteValue = false });

        // Act
        var result = composite.CanExecute(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Execute_ShouldExecuteAllCommands()
    {
        // Arrange
        var composite = new CompositeCommand();
        var command1 = new MockCommand();
        var command2 = new MockCommand();
        composite.RegisterCommand(command1);
        composite.RegisterCommand(command2);

        // Act
        composite.Execute("test");

        // Assert
        Assert.Equal(1, command1.ExecuteCount);
        Assert.Equal(1, command2.ExecuteCount);
        Assert.Equal("test", command1.LastParameter);
    }

    [Fact]
    public void Execute_ShouldSkipCommandsThatCannotExecute()
    {
        // Arrange
        var composite = new CompositeCommand();
        var command1 = new MockCommand { CanExecuteValue = true };
        var command2 = new MockCommand { CanExecuteValue = false };
        composite.RegisterCommand(command1);
        composite.RegisterCommand(command2);

        // Act
        composite.Execute(null);

        // Assert
        Assert.Equal(1, command1.ExecuteCount);
        Assert.Equal(0, command2.ExecuteCount);
    }

    [Fact]
    public void CanExecuteChanged_ShouldBeRaisedOnRegister()
    {
        // Arrange
        var composite = new CompositeCommand();
        var raised = false;
        composite.CanExecuteChanged += (_, _) => raised = true;

        // Act
        composite.RegisterCommand(new MockCommand());

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public void CanExecuteChanged_ShouldBeRaisedOnUnregister()
    {
        // Arrange
        var composite = new CompositeCommand();
        var command = new MockCommand();
        composite.RegisterCommand(command);
        var raised = false;
        composite.CanExecuteChanged += (_, _) => raised = true;

        // Act
        composite.UnregisterCommand(command);

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public void CanExecuteChanged_ShouldBeRaisedWhenChildCommandChanges()
    {
        // Arrange
        var composite = new CompositeCommand();
        var command = new MockCommand();
        composite.RegisterCommand(command);
        var raised = false;
        composite.CanExecuteChanged += (_, _) => raised = true;

        // Act
        command.RaiseCanExecuteChanged();

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public void CanExecuteChanged_ShouldNotBeRaisedAfterUnregister()
    {
        // Arrange
        var composite = new CompositeCommand();
        var command = new MockCommand();
        composite.RegisterCommand(command);
        composite.UnregisterCommand(command);
        var raised = false;
        composite.CanExecuteChanged += (_, _) => raised = true;

        // Act
        command.RaiseCanExecuteChanged();

        // Assert
        Assert.False(raised);
    }

    [Fact]
    public void MonitorCommandActivity_InactiveCommand_ShouldBeSkipped()
    {
        // Arrange
        var composite = new CompositeCommand(monitorCommandActivity: true);
        var activeCommand = new ActiveAwareCommand { IsActive = true };
        var inactiveCommand = new ActiveAwareCommand { IsActive = false };
        composite.RegisterCommand(activeCommand);
        composite.RegisterCommand(inactiveCommand);

        // Act
        composite.Execute(null);

        // Assert
        Assert.Equal(1, activeCommand.ExecuteCount);
        Assert.Equal(0, inactiveCommand.ExecuteCount);
    }

    [Fact]
    public void MonitorCommandActivity_InactiveCommand_CanExecuteShouldIgnore()
    {
        // Arrange
        var composite = new CompositeCommand(monitorCommandActivity: true);
        var activeCommand = new ActiveAwareCommand { IsActive = true, CanExecuteValue = true };
        var inactiveCommand = new ActiveAwareCommand { IsActive = false, CanExecuteValue = false };
        composite.RegisterCommand(activeCommand);
        composite.RegisterCommand(inactiveCommand);

        // Act - inactive command's CanExecute should be ignored
        var result = composite.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MonitorCommandActivity_False_AllCommandsConsidered()
    {
        // Arrange
        var composite = new CompositeCommand(monitorCommandActivity: false);
        var activeCommand = new ActiveAwareCommand { IsActive = true, CanExecuteValue = true };
        var inactiveCommand = new ActiveAwareCommand { IsActive = false, CanExecuteValue = false };
        composite.RegisterCommand(activeCommand);
        composite.RegisterCommand(inactiveCommand);

        // Act - both commands should be considered
        var result = composite.CanExecute(null);

        // Assert - should be false because inactiveCommand cannot execute
        Assert.False(result);
    }

    [Fact]
    public void RaiseCanExecuteChanged_ShouldRaiseEvent()
    {
        // Arrange
        var composite = new CompositeCommand();
        var raised = false;
        composite.CanExecuteChanged += (_, _) => raised = true;

        // Act
        composite.RaiseCanExecuteChanged();

        // Assert
        Assert.True(raised);
    }
}
