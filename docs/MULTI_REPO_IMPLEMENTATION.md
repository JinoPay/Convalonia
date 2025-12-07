# Multi-Repository Workspace Implementation

## Overview
This implementation adds support for multiple git repositories within a single workspace, allowing users to manage and work with several repositories simultaneously.

## Key Features

### 1. Repository Model (`src/Models/Repository.cs`)
Each repository contains:
- **RootPath**: Original git repository path (if copied from local)
- **WorkspacePath**: Path within the workspace where this repository lives
- **CurrentBranch**: Current branch name
- **BaseBranch**: Branch to create new workspace from
- **RemoteOrigin**: Remote origin URL
- **SearchArchivedBranches**: Whether to include archived branches in search
- **HasChanges**: Indicates uncommitted changes
- **LastCommitHash**: Latest commit hash

### 2. RepositoryService (`src/Services/RepositoryService.cs`)
Manages repository operations:
- **AddLocalRepositoryAsync**: Add repository from local git project
- **AddRepositoryFromUrlAsync**: Clone repository from URL
- **CreateBranchAsync**: Create new branch with optional base branch
- **CheckoutBranchAsync**: Switch to different branch
- **GetBranchesAsync**: Get all branches (including archived if specified)
- **UpdateRepositoryStatusAsync**: Update repository status (changes, commit hash)
- **RemoveRepositoryAsync**: Remove repository from workspace

### 3. Three Ways to Add Repositories

#### a. Open Project
- Select an existing local git repository
- The repository is copied to the workspace directory
- Preserves all git history and branches

#### b. Clone From URL
- Provide a git repository URL
- Repository is cloned into the workspace
- Optionally specify a branch to checkout
- Option to search archived branches

#### c. Quick Start
- Creates an empty git repository
- Good for starting new projects
- (Currently placeholder - to be implemented)

### 4. Branch Management (`src/ViewModels/BranchSelectorViewModel.cs`)
Features:
- Search through branches
- Filter branches by name
- Toggle inclusion of archived branches
- Create new branches from selected base branch
- Checkout existing branches

### 5. UI Components

#### WorkspaceView Updates (`src/Views/WorkspaceView.axaml`)
- New "Repositories" section in sidebar
- Shows all repositories with:
  - Repository name
  - Current branch
  - Quick actions (Open folder, Refresh status)
  - Remove button

#### AddRepositoryDialog (`src/Views/AddRepositoryDialog.axaml`)
- Modal dialog for adding repositories
- Three methods: Open Project, Clone From URL, Quick Start
- Method-specific input fields
- Option to search archived branches

#### BranchSelectorDialog (`src/Views/BranchSelectorDialog.axaml`)
- Search and filter branches
- Toggle archived branch visibility
- Create new branches
- Checkout branches

## Updated Services

### GitHubService Extensions (`src/Services/GitHubService.cs`)
Added methods:
- `CreateBranchAsync`: Now supports base branch parameter
- `CheckoutBranchAsync`: Switch to specific branch
- `GetBranchesAsync`: Get all branches (local and remote)
- `GetRemoteOriginAsync`: Get remote origin URL
- `HasUncommittedChangesAsync`: Check for uncommitted changes
- `GetLastCommitHashAsync`: Get latest commit hash
- `ExecuteGitCommandAsync`: Helper method for git operations

### WorkspaceService Updates (`src/Services/WorkspaceService.cs`)
- Added `RepositoryService` property
- Workspaces now support multiple repositories
- Maintains backward compatibility with legacy properties

## Workspace Model Updates (`src/Models/Workspace.cs`)
- Added `Repositories` collection
- Kept legacy `GitBranch` and `GitRemote` properties for backward compatibility

## Usage Flow

1. **Create Workspace**: User creates a new workspace
2. **Add Repositories**: User adds one or more repositories via:
   - Opening an existing local git project
   - Cloning from a URL
   - Quick starting a new repository
3. **Manage Branches**: For each repository:
   - Search and filter branches
   - Create new branches from base
   - Checkout branches
   - View archived branches
4. **Work with Agents**: Agents can work across all repositories in the workspace

## Configuration Options

### Per-Repository Settings
- **SearchArchivedBranches**: Include archived branches in branch listings
- **BaseBranch**: Default branch for creating new branches

## Future Enhancements
1. Quick Start implementation (empty git repo initialization)
2. Repository synchronization (fetch, pull, push)
3. Commit and push directly from UI
4. Branch comparison and merging
5. Repository-specific agent assignments
6. Multi-repository operations (update all, status all)

## Technical Notes

### Git Operations
All git operations are executed via `ProcessStartInfo` with:
- Redirected output/error streams
- No shell execution
- No window creation
- Proper error handling

### Branch Detection
- Local branches: `git branch`
- Remote branches: `git branch -a`
- Archived branches included when flag is set

### Repository Copying
Uses `git clone` for local repositories to preserve:
- Full git history
- All branches
- Tags and refs
- Remote configuration
