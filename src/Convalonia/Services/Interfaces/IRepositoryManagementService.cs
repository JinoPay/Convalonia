using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Convalonia.Models;

namespace Convalonia.Services;

/// <summary>
/// Interface for source repository management
/// </summary>
public interface IRepositoryManagementService
{
    ObservableCollection<SourceRepository> Repositories { get; }

    Task InitializeAsync();
    Task<SourceRepository> AddLocalRepositoryAsync(string localPath);
    Task<SourceRepository> AddRemoteRepositoryAsync(string gitUrl);
    Task<SourceRepository> CreateNewRepositoryAsync(string folderPath);
    Task RemoveRepositoryAsync(Guid repositoryId);
    Task UpdateLastAccessedAsync(Guid repositoryId);
    Task<Workspace> CreateWorkspaceAsync(SourceRepository repository, string? workspaceName = null);
}
