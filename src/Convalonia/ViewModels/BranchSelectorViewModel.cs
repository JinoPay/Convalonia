using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Convalonia.Models;
using Convalonia.Services;
using Jinobald.Core.Mvvm;
using Jinobald.Core.Services.Toast;

namespace Convalonia.ViewModels;

/// <summary>
/// ViewModel for selecting and searching branches
/// </summary>
public partial class BranchSelectorViewModel : ViewModelBase
{
    private readonly Repository _repository;
    private readonly RepositoryService _repositoryService;
    private readonly IToastService _toastService;

    [ObservableProperty]
    private ObservableCollection<string> _branches = new();

    [ObservableProperty]
    private ObservableCollection<string> _filteredBranches = new();

    [ObservableProperty]
    private string? _selectedBranch;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _includeArchived = false;

    [ObservableProperty]
    private bool _isLoading = false;

    public BranchSelectorViewModel(
        Repository repository,
        RepositoryService repositoryService,
        IToastService toastService)
    {
        _repository = repository;
        _repositoryService = repositoryService;
        _toastService = toastService;
        _includeArchived = repository.SearchArchivedBranches;

        _ = LoadBranchesAsync();
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterBranches();
    }

    partial void OnIncludeArchivedChanged(bool value)
    {
        _repository.SearchArchivedBranches = value;
        _ = LoadBranchesAsync();
    }

    [RelayCommand]
    private async Task LoadBranchesAsync()
    {
        IsLoading = true;

        try
        {
            var branches = await _repositoryService.GetBranchesAsync(_repository, IncludeArchived);
            Branches.Clear();

            foreach (var branch in branches)
            {
                Branches.Add(branch);
            }

            FilterBranches();
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to load branches: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CheckoutBranchAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedBranch))
        {
            _toastService.ShowError("Please select a branch");
            return;
        }

        IsLoading = true;

        try
        {
            var success = await _repositoryService.CheckoutBranchAsync(_repository, SelectedBranch);

            if (success)
            {
                _toastService.ShowSuccess($"Checked out branch '{SelectedBranch}'");
            }
            else
            {
                _toastService.ShowError($"Failed to checkout branch '{SelectedBranch}'");
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to checkout branch: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateNewBranchAsync(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            _toastService.ShowError("Please enter a branch name");
            return;
        }

        IsLoading = true;

        try
        {
            var baseBranch = SelectedBranch ?? _repository.BaseBranch;
            var success = await _repositoryService.CreateBranchAsync(_repository, branchName, baseBranch);

            if (success)
            {
                _toastService.ShowSuccess($"Created and checked out branch '{branchName}'");
                await LoadBranchesAsync();
                SelectedBranch = branchName;
            }
            else
            {
                _toastService.ShowError($"Failed to create branch '{branchName}'");
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Failed to create branch: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterBranches()
    {
        FilteredBranches.Clear();

        var query = SearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;

        var filtered = string.IsNullOrWhiteSpace(query)
            ? Branches
            : Branches.Where(b => b.ToLowerInvariant().Contains(query));

        foreach (var branch in filtered)
        {
            FilteredBranches.Add(branch);
        }
    }
}
