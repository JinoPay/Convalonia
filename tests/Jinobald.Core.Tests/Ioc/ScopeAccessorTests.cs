using Jinobald.Core.Ioc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jinobald.Core.Tests.Ioc;

public class ScopeAccessorTests
{
    public interface ITestService
    {
        Guid Id { get; }
    }

    public class TransientService : ITestService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class ScopedService : ITestService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class SingletonService : ITestService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private static (ScopeAccessor, IContainerExtension) CreateAccessor()
    {
        var services = new ServiceCollection();
        services.AddTransient<TransientService>();
        services.AddScoped<ScopedService>();
        services.AddSingleton<SingletonService>();

        var container = services.AsContainerExtension();
        container.FinalizeExtension();

        var accessor = new ScopeAccessor(container);
        return (accessor, container);
    }

    [Fact]
    public void CurrentScope_WithoutScope_ShouldBeNull()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Assert
        Assert.Null(accessor.CurrentScope);
    }

    [Fact]
    public void CreateScope_ShouldSetCurrentScope()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Act
        using var context = accessor.CreateScope();

        // Assert
        Assert.NotNull(accessor.CurrentScope);
        Assert.Same(context.Scope, accessor.CurrentScope);
    }

    [Fact]
    public void CreateScope_AfterDispose_ShouldRestorePreviousScope()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Act
        using (accessor.CreateScope())
        {
            Assert.NotNull(accessor.CurrentScope);
        }

        // Assert
        Assert.Null(accessor.CurrentScope);
    }

    [Fact]
    public void CreateScope_Nested_ShouldRestoreCorrectScope()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Act
        using var outerScope = accessor.CreateScope("outer");
        var outerScopeRef = accessor.CurrentScope;

        using (var innerScope = accessor.CreateScope("inner"))
        {
            Assert.NotSame(outerScopeRef, accessor.CurrentScope);
            Assert.Same(innerScope.Scope, accessor.CurrentScope);
        }

        // Assert - should restore to outer scope
        Assert.Same(outerScopeRef, accessor.CurrentScope);
    }

    [Fact]
    public void Resolve_WithoutScope_ShouldResolveFromContainer()
    {
        // Arrange
        var (accessor, container) = CreateAccessor();

        // Act
        var service1 = accessor.Resolve<SingletonService>();
        var service2 = container.Resolve<SingletonService>();

        // Assert
        Assert.Same(service1, service2); // Singleton should be same
    }

    [Fact]
    public void Resolve_WithScope_ScopedService_ShouldBeSameWithinScope()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Act
        using (accessor.CreateScope())
        {
            var service1 = accessor.Resolve<ScopedService>();
            var service2 = accessor.Resolve<ScopedService>();

            // Assert
            Assert.Same(service1, service2);
        }
    }

    [Fact]
    public void Resolve_DifferentScopes_ScopedService_ShouldBeDifferent()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();
        ScopedService? service1 = null;
        ScopedService? service2 = null;

        // Act
        using (accessor.CreateScope())
        {
            service1 = accessor.Resolve<ScopedService>();
        }

        using (accessor.CreateScope())
        {
            service2 = accessor.Resolve<ScopedService>();
        }

        // Assert
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void Resolve_TransientService_ShouldAlwaysBeNew()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Act
        using (accessor.CreateScope())
        {
            var service1 = accessor.Resolve<TransientService>();
            var service2 = accessor.Resolve<TransientService>();

            // Assert
            Assert.NotSame(service1, service2);
        }
    }

    [Fact]
    public void Resolve_ByType_ShouldWork()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Act
        using (accessor.CreateScope())
        {
            var service = accessor.Resolve(typeof(ScopedService));

            // Assert
            Assert.NotNull(service);
            Assert.IsType<ScopedService>(service);
        }
    }

    [Fact]
    public void CreateScope_WithName_ShouldWork()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Act
        using var context = accessor.CreateScope("TestScope");

        // Assert
        Assert.NotNull(accessor.CurrentScope);
    }

    [Fact]
    public void StaticCurrent_ShouldMatchCurrentScope()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Act
        using var context = accessor.CreateScope();

        // Assert
        Assert.Same(ScopeAccessor.Current, accessor.CurrentScope);
    }

    [Fact]
    public async Task CreateScope_ShouldWorkAcrossAsyncCalls()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Act
        using (accessor.CreateScope())
        {
            var scopeBefore = accessor.CurrentScope;
            await Task.Yield(); // Force async context switch
            var scopeAfter = accessor.CurrentScope;

            // Assert - scope should be maintained across async calls
            Assert.Same(scopeBefore, scopeAfter);
        }
    }

    [Fact]
    public async Task CreateScope_NestedAsync_ShouldMaintainCorrectScopes()
    {
        // Arrange
        var (accessor, _) = CreateAccessor();

        // Act
        using (var outer = accessor.CreateScope("outer"))
        {
            var outerService = accessor.Resolve<ScopedService>();

            await Task.Yield();

            using (accessor.CreateScope("inner"))
            {
                var innerService = accessor.Resolve<ScopedService>();
                Assert.NotSame(outerService, innerService);

                await Task.Yield();
            }

            // After inner scope disposes, should still get outer scoped service
            var afterInner = accessor.Resolve<ScopedService>();
            Assert.Same(outerService, afterInner);
        }
    }
}
