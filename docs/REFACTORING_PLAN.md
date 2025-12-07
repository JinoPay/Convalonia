# Convalonia 전면 리팩터링 계획

> **목표**: Conductor 유사 프로그램을 완전하고 안전하며 프로덕션 레벨로 구축

## 📊 현재 상태 분석

- **총 코드 라인**: ~4,510 라인 (C#)
- **아키텍처 품질**: 7/10 - MVVM 구조는 좋으나 결합도 문제
- **코드 품질**: 6.5/10 - 깨끗하나 안티패턴 존재
- **완성도**: 60% - 핵심 기능 구현, 다수 TODO 존재
- **보안**: 5/10 - **심각한 취약점 발견**
- **성능**: 6/10 - 메모리 누수 및 블로킹 호출 문제

## 🎯 Conductor 핵심 기능 분석

### 필수 구현 기능
1. **워크스페이스 관리**
   - Git 파일 복사 (tracked files only)
   - 브랜치별 독립 워크스페이스
   - 하나의 브랜치 = 하나의 워크스페이스

2. **Scripts (conductor.json)**
   - `setup`: 워크스페이스 생성 시 실행 (npm install, .env 복사 등)
   - `run`: 개발 서버 실행 버튼
   - `archive`: 워크스페이스 삭제 시 정리
   - `runScriptMode`: "nonconcurrent" 지원

3. **환경 변수**
   - `$CONDUCTOR_WORKSPACE_PATH`: 워크스페이스 경로
   - `$CONDUCTOR_ROOT_PATH`: 리포지토리 루트
   - `$CONDUCTOR_PORT`: 할당된 포트 (10개 포트 범위)
   - `$CONDUCTOR_WORKSPACE_NAME`: 워크스페이스 이름

4. **병렬 에이전트**
   - ⌘N으로 새 워크스페이스 생성
   - 각 워크스페이스는 독립적 Claude 실행
   - 상호 간섭 없음

5. **Checkpoints**
   - 턴별 자동 스냅샷
   - 이전 턴으로 되돌리기 (영구 삭제)
   - Private Git ref에 커밋
   - 로컬 저장 (working branch와 분리)

6. **Diff Viewer** (⌘D)
   - Claude가 만든 변경사항 확인
   - GitHub 동기화
   - PR 생성 워크플로우

7. **워크플로우**
   - 1 workspace = 1 feature/bugfix
   - 개발 → 리뷰/테스트 → PR 생성 (⌘⇧P) → 머지 → 아카이브

---

## 📋 작업 계획 (총 51개 + Conductor 기능 추가)

### 🚨 Phase 1: CRITICAL Issues (즉시 수정 필요) - 5개

#### 1. Command Injection 취약점 수정
**파일**: `src/Services/GitHubService.cs:197`
```csharp
// 현재 (취약):
Arguments = $"commit -m \"{message}\""

// 수정 후:
Arguments = $"commit -m {EscapeGitArgument(message)}"
```
**영향**: 공격자가 임의의 명령 실행 가능 (`"; rm -rf / #"`)

#### 2. Path Traversal 취약점 수정
**파일**: `src/Services/FileSystemService.cs:18`
```csharp
// 추가 필요:
private void ValidatePathInWorkspace(string filePath, string workspacePath)
{
    var fullPath = Path.GetFullPath(filePath);
    var workspaceFullPath = Path.GetFullPath(workspacePath);
    if (!fullPath.StartsWith(workspaceFullPath))
        throw new SecurityException("Path traversal detected");
}
```

#### 3. Deadlock 수정 - RepositoryManagementService
**파일**: `src/Services/RepositoryManagementService.cs:39`
```csharp
// 현재 (데드락 위험):
public RepositoryManagementService(...)
{
    LoadRepositoriesAsync().Wait(); // UI 스레드 블로킹
}

// 수정 후: Factory 패턴 사용
public static async Task<RepositoryManagementService> CreateAsync(...)
{
    var service = new RepositoryManagementService(...);
    await service.LoadRepositoriesAsync();
    return service;
}
```

#### 4. Deadlock 수정 - ClaudeCodeService.Dispose
**파일**: `src/Services/ClaudeCodeService.cs:226`
```csharp
// 현재 (데드락 위험):
public void Dispose()
{
    StopSessionAsync().Wait();
}

// 수정 후:
public void Dispose()
{
    StopSessionAsync().GetAwaiter().GetResult();
}
```

#### 5. Runtime Crash 수정 - AgentSelectionConverter
**파일**: `src/Views/UnifiedMainView.axaml:242`
```xml
<!-- 현재 (존재하지 않음): -->
Converter={StaticResource AgentSelectionConverter}

<!-- 수정: Converter 구현 또는 제거 -->
```

---

### ⚠️ Phase 2: HIGH Priority Issues - 8개

#### 6. Process 리소스 누수 수정
**파일**: `src/Services/ClaudeCodeService.cs:186-192`
```csharp
// try-finally로 감싸서 확실한 Dispose 보장
```

#### 7. HttpClient 안티패턴 수정
**파일**: `src/Services/ClaudeApiService.cs:24`
```csharp
// IHttpClientFactory 사용하도록 변경
// App.axaml.cs에 services.AddHttpClient<ClaudeApiService>() 추가
```

#### 8. async void 이벤트 핸들러 수정
**파일**:
- `src/ViewModels/UnifiedMainViewModel.cs:282`
- `src/ViewModels/WorkspaceViewModel.cs:123`
```csharp
// try-catch 추가하여 예외 처리
private async void OnFirstMessageSent(object? sender, string firstMessage)
{
    try { ... }
    catch (Exception ex) { _logger.LogError(ex, "..."); }
}
```

#### 9-13. DI, 입력 검증, 로깅, 전역 예외 처리
- 서비스 인터페이스 추가
- 입력 검증 (URL, 경로, 브랜치명, 메시지)
- Serilog 도입
- App.axaml.cs에 전역 예외 핸들러

---

### 🔧 Phase 3: Architecture Refactoring - 12개

#### 14. ChatViewModel DI 리팩터링
```csharp
// 현재:
_claudeCodeService = new ClaudeCodeService(workspacePath);

// 수정 후:
public ChatViewModel(IClaudeCodeServiceFactory factory, ...)
{
    _claudeCodeService = factory.Create(workspacePath);
}
```

#### 15. FluentValidation 추가
```csharp
public class RepositoryValidator : AbstractValidator<Repository>
{
    public RepositoryValidator()
    {
        RuleFor(r => r.Path).NotEmpty().Must(BeValidPath);
        RuleFor(r => r.GitUrl).Must(BeValidGitUrl).When(r => !string.IsNullOrEmpty(r.GitUrl));
    }
}
```

#### 16. 스레드 안전성 - ClaudeCodeService.IsRunning
```csharp
private readonly object _processLock = new();
public bool IsRunning
{
    get
    {
        lock (_processLock)
            return _process != null && !_process.HasExited;
    }
}
```

#### 17-25. 에러 처리 개선, CancellationToken, 성능 최적화 등

---

### 🎨 Phase 4: UI/UX Completion - 10개

#### 23. AI 모델 선택 연결
**파일**: `src/Views/UnifiedMainView.axaml:328-334`
```xml
<ComboBox SelectedItem="{Binding SelectedAgent.Model}"
          ItemsSource="{Binding AvailableModels}">
```

#### 24. Run 버튼 구현
**파일**: `src/ViewModels/UnifiedMainViewModel.cs`
```csharp
[RelayCommand]
private async Task RunWorkspaceAsync()
{
    // conductor.json의 run script 실행
    await _scriptRunner.RunScriptAsync(SelectedWorkspace, "run");
}
```

#### 25. Terminal 버튼 구현
```csharp
[RelayCommand]
private void ToggleTerminal()
{
    IsTerminalVisible = !IsTerminalVisible;
}
```

#### 26. Files 탭 구현 (변경사항, 전체 파일)
- Git diff로 변경된 파일 목록
- 전체 파일 트리 표시

#### 27-35. 자동 스크롤, 로딩 표시, 파일 피커 등

---

### 💾 Phase 5: Persistence & State - 3개

#### 36. 워크스페이스 영속성
```json
// workspace-{id}.json
{
  "id": "...",
  "name": "...",
  "repositories": [...],
  "agents": [...],
  "createdAt": "...",
  "lastAccessedAt": "..."
}
```

#### 37. 에이전트 대화 영속성
```json
// agent-{id}-messages.json
{
  "agentId": "...",
  "messages": [
    { "role": "user", "content": "...", "timestamp": "..." },
    { "role": "assistant", "content": "...", "timestamp": "..." }
  ]
}
```

#### 38. 세션 복원
- 마지막 열었던 워크스페이스 복원
- Claude Code 프로세스 상태 복원

---

### ⚙️ Phase 6: Conductor Scripts 구현 - 8개

#### 39. conductor.json 파싱
```csharp
public class ConductorConfig
{
    public ConductorScripts? Scripts { get; set; }
    public string? RunScriptMode { get; set; } // "nonconcurrent"
}

public class ConductorScripts
{
    public string? Setup { get; set; }
    public string? Run { get; set; }
    public string? Archive { get; set; }
}
```

#### 40. Setup Script 실행
```csharp
public async Task ExecuteSetupScriptAsync(Workspace workspace)
{
    var config = await LoadConductorConfigAsync(workspace.Path);
    if (config?.Scripts?.Setup == null) return;

    var env = new Dictionary<string, string>
    {
        ["CONDUCTOR_WORKSPACE_PATH"] = workspace.Path,
        ["CONDUCTOR_ROOT_PATH"] = workspace.RootPath,
        ["CONDUCTOR_WORKSPACE_NAME"] = workspace.Name
    };

    await RunShellScriptAsync(config.Scripts.Setup, workspace.Path, env);
}
```

#### 41. Run Script 실행 (nonconcurrent 지원)
```csharp
private Process? _runningScript;

public async Task ExecuteRunScriptAsync(Workspace workspace)
{
    var config = await LoadConductorConfigAsync(workspace.Path);

    if (config?.RunScriptMode == "nonconcurrent" && _runningScript != null)
    {
        _runningScript.Kill();
        _runningScript = null;
    }

    var port = AllocatePort(workspace);
    var env = new Dictionary<string, string>
    {
        ["CONDUCTOR_PORT"] = port.ToString(),
        // ... other vars
    };

    _runningScript = await RunShellScriptAsync(config.Scripts.Run, workspace.Path, env);
}
```

#### 42. Archive Script 실행
```csharp
public async Task ExecuteArchiveScriptAsync(Workspace workspace)
{
    var config = await LoadConductorConfigAsync(workspace.Path);
    if (config?.Scripts?.Archive == null) return;

    await RunShellScriptAsync(config.Scripts.Archive, workspace.Path, env);
}
```

#### 43. Port 할당 시스템
```csharp
public class PortAllocator
{
    private const int BasePort = 3000;
    private readonly Dictionary<string, int> _workspacePorts = new();

    public int AllocatePort(Workspace workspace)
    {
        if (_workspacePorts.TryGetValue(workspace.Id, out var port))
            return port;

        var newPort = BasePort + (_workspacePorts.Count * 10);
        _workspacePorts[workspace.Id] = newPort;
        return newPort;
    }
}
```

#### 44. ANSI 색상 지원
```csharp
// AnsiTextBlock 구현 (Avalonia.AnsiText NuGet 사용)
```

#### 45-46. 템플릿, 히스토리 로깅

---

### 🚀 Phase 7: Checkpoints 구현 - 4개

#### 47. Checkpoint 시스템 설계
```csharp
public class Checkpoint
{
    public string Id { get; init; }
    public string WorkspaceId { get; init; }
    public string AgentId { get; init; }
    public int TurnNumber { get; init; }
    public string GitCommitSha { get; init; }
    public DateTime CreatedAt { get; init; }
    public string UserMessage { get; init; }
    public string AssistantMessage { get; init; }
}
```

#### 48. Private Git Ref 저장
```csharp
public async Task<string> CreateCheckpointAsync(Workspace workspace, int turnNumber)
{
    var refName = $"refs/conductor/checkpoints/{workspace.Id}/turn-{turnNumber}";

    // 현재 작업 트리 커밋
    await _gitService.CommitAsync(workspace.Path, $"Checkpoint turn {turnNumber}",
        skipHooks: true);
    var sha = await _gitService.GetCurrentCommitShaAsync(workspace.Path);

    // Private ref에 저장
    await _gitService.UpdateRefAsync(workspace.Path, refName, sha);

    return sha;
}
```

#### 49. Checkpoint 되돌리기
```csharp
public async Task RevertToCheckpointAsync(Checkpoint checkpoint)
{
    // Git reset --hard to checkpoint SHA
    await _gitService.ResetHardAsync(workspace.Path, checkpoint.GitCommitSha);

    // 메시지 삭제 (checkpoint 이후 모든 메시지)
    agent.Messages.RemoveRange(checkpoint.TurnNumber,
        agent.Messages.Count - checkpoint.TurnNumber);

    // UI 업데이트
    NotifyCheckpointReverted(checkpoint);
}
```

#### 50. Checkpoint UI
```xml
<!-- ChatView.axaml에 Revert 버튼 추가 -->
<Button Command="{Binding RevertToCheckpointCommand}"
        CommandParameter="{Binding TurnNumber}"
        IsVisible="{Binding $parent[ListBoxItem].IsPointerOver}">
    ⟲ Revert
</Button>
```

---

### 🎯 Phase 8: Diff Viewer & PR 생성 - 3개

#### 51. Diff Viewer 구현
```csharp
public class DiffViewerViewModel : ViewModelBase
{
    public ObservableCollection<FileDiff> ChangedFiles { get; }

    public async Task LoadDiffsAsync()
    {
        var diff = await _gitService.GetDiffAsync(workspace.Path, "main...HEAD");
        ChangedFiles.Clear();
        foreach (var file in ParseDiff(diff))
            ChangedFiles.Add(file);
    }
}
```

#### 52. PR 생성 자동화
```csharp
[RelayCommand]
private async Task CreatePullRequestAsync()
{
    // 1. Push to remote
    await _gitService.PushAsync(workspace.Path);

    // 2. Create PR via GitHub CLI
    var prUrl = await _gitHubService.CreatePullRequestAsync(
        repository: workspace.Repository,
        title: GeneratePRTitle(),
        body: GeneratePRBody(),
        baseBranch: "main",
        headBranch: workspace.CurrentBranch
    );

    _toastService.ShowSuccess($"PR created: {prUrl}");
}
```

#### 53. Agent 간 작업 공유
```csharp
public class AgentTaskSharingService
{
    public async Task ShareTaskAsync(Agent sourceAgent, Agent targetAgent, string taskDescription)
    {
        var sharedTask = new AgentTask
        {
            Description = taskDescription,
            SourceAgentId = sourceAgent.Id,
            Status = AgentTaskStatus.Pending
        };

        targetAgent.Tasks.Add(sharedTask);
        await _toastService.ShowInfoAsync($"Task shared to {targetAgent.Name}");
    }
}
```

---

### ✅ Phase 9: Testing & Quality - 5개

#### 54. 단위 테스트 프로젝트
```bash
dotnet new xunit -n Convalonia.Tests
dotnet add reference ../Convalonia/Convalonia.csproj
dotnet add package Moq
dotnet add package FluentAssertions
```

#### 55. Service Mock
```csharp
public class GitHubServiceTests
{
    private readonly Mock<IProcessRunner> _processRunnerMock;
    private readonly GitHubService _sut;

    [Fact]
    public async Task CloneAsync_ValidUrl_CreatesRepository()
    {
        // Arrange
        _processRunnerMock.Setup(x => x.RunAsync(It.IsAny<string>(), ...))
            .ReturnsAsync((0, "", ""));

        // Act
        var result = await _sut.CloneAsync("https://github.com/user/repo", "/path");

        // Assert
        result.Should().BeTrue();
    }
}
```

#### 56-58. 통합 테스트, 에러 시나리오 테스트, C# 최신 기능 활용

---

### 📚 Phase 10: Documentation & Final Polish - 3개

#### 59. XML 문서화 주석
```csharp
/// <summary>
/// Clones a Git repository to the specified target path.
/// </summary>
/// <param name="repoUrl">The Git repository URL (HTTPS or SSH).</param>
/// <param name="targetPath">The local directory path where the repository will be cloned.</param>
/// <param name="cancellationToken">Cancellation token for async operation.</param>
/// <returns>True if clone succeeded, false otherwise.</returns>
/// <exception cref="ArgumentException">Thrown when repoUrl or targetPath is invalid.</exception>
/// <exception cref="GitException">Thrown when git clone fails.</exception>
public async Task<bool> CloneAsync(string repoUrl, string targetPath,
    CancellationToken cancellationToken = default)
```

#### 60. README.md 업데이트
- 설치 가이드
- 스크린샷 추가
- conductor.json 예제
- 트러블슈팅 섹션

#### 61. 기여 가이드 (CONTRIBUTING.md)
- 코드 스타일 가이드
- PR 프로세스
- 이슈 템플릿

---

## 🔄 작업 진행 방식

### 병렬 작업 전략
1. **보안 이슈 (1-2)**: 최우선 - 단일 작업자
2. **데드락 이슈 (3-4)**: 독립적 - 병렬 가능
3. **DI & 아키텍처 (9-14)**: 기반 작업 - 순차 진행
4. **UI 기능 (23-26)**: 독립적 - 병렬 가능
5. **Scripts 구현 (39-43)**: 순차 진행 (의존성 있음)
6. **Checkpoints (47-50)**: 순차 진행
7. **테스트 (54-58)**: 각 Phase 완료 후 병렬 작성

### 진행 순서
```
Week 1: Phase 1 (CRITICAL) → Phase 2 (HIGH)
Week 2: Phase 3 (Architecture)
Week 3: Phase 4 (UI) + Phase 5 (Persistence)
Week 4: Phase 6 (Scripts) + Phase 7 (Checkpoints)
Week 5: Phase 8 (Diff & PR) + Phase 9 (Testing)
Week 6: Phase 10 (Documentation) + 최종 테스트
```

---

## 📊 성공 기준

### 필수 요구사항
- [ ] 모든 CRITICAL 보안 이슈 수정
- [ ] 모든 데드락 위험 제거
- [ ] conductor.json 완전 지원 (setup/run/archive)
- [ ] Checkpoints 시스템 작동
- [ ] Diff Viewer & PR 생성 작동
- [ ] 단위 테스트 커버리지 > 70%

### 품질 목표
- [ ] 코드 품질 > 8.5/10
- [ ] 보안 > 9/10
- [ ] 성능 > 8/10
- [ ] 완성도 > 95%

---

## 🛠️ 기술 스택 추가

### 현재
- .NET 9.0
- Avalonia UI 11.3.9
- Jinobald Framework + CommunityToolkit.Mvvm

### 추가 필요
- **Serilog**: 로깅
- **FluentValidation**: 입력 검증
- **Polly**: 재시도 로직
- **xUnit + Moq + FluentAssertions**: 테스트
- **Avalonia.AnsiText**: ANSI 색상 (또는 커스텀 구현)

---

## 📝 참고 문서
- [Conductor 공식 문서](https://docs.conductor.build)
- [Your First Workspace](https://docs.conductor.build/first-workspace)
- [Conductor Workflow](https://docs.conductor.build/workflow)
- [Scripts Documentation](https://docs.conductor.build/core/scripts)
- [Checkpoints](https://docs.conductor.build/core/checkpoints)
- [Parallel Agents](https://docs.conductor.build/core/parallel-agents)

---

## 🎯 즉시 시작할 작업

### Phase 1 (완료)
1. ✅ Command Injection 수정 (GitHubService.cs)
2. ✅ Path Traversal 수정 (FileSystemService.cs)
3. ✅ Deadlock 수정 (RepositoryManagementService.cs)
4. ✅ Deadlock 수정 (ClaudeCodeService.cs)
5. ✅ AgentSelectionConverter 추가 (UnifiedMainView.axaml)

### Phase 2 (진행 중)
6. ✅ Process 리소스 누수 수정 (ClaudeCodeService.cs) - 스레드 안전성, Dispose 패턴 개선
7. ✅ HttpClient 안티패턴 수정 (IClaudeApiService 인터페이스 추가)
8. ✅ async void 이벤트 핸들러 수정 (try-catch 추가)
9. ✅ DI 인터페이스 추가 (IGitService, IWorkspaceService, IRepositoryService 등)
10. ✅ 전역 예외 처리 추가 (App.axaml.cs)
11. ⏳ 입력 검증 추가 (URL, 경로, 브랜치명)
12. ⏳ Serilog 로깅 도입

**다음 단계**: Phase 2 완료 후 Phase 3 Architecture Refactoring 시작
