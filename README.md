# Convalonia

Conductor와 유사한 Claude 병렬 작업 프로그램입니다. 여러 개의 워크스페이스에서 Claude 에이전트들을 동시에 실행하여 병렬로 작업할 수 있습니다.

## 주요 기능

- **워크스페이스 관리**: 독립적인 작업 환경을 여러 개 생성하고 관리
- **병렬 에이전트 실행**: 각 워크스페이스에서 여러 Claude 에이전트를 동시에 실행
- **Claude Code CLI 통합**: Claude Code CLI와 프로세스 통신으로 실시간 작업 수행
- **터미널 출력 표시**: Claude의 작업 과정을 실시간으로 터미널 출력으로 확인
- **GitHub 연동**: 리포지토리 클론, 브랜치 관리, 커밋, 푸시
- **파일 시스템 관리**: 워크스페이스 내 파일 읽기/쓰기/검색

## 프로젝트 구조

```
guangzhou/
├── src/                         # 소스 코드
│   └── Convalonia/             # 메인 애플리케이션
│       ├── Models/             # 데이터 모델
│       ├── Services/           # 비즈니스 로직
│       ├── ViewModels/         # MVVM 뷰모델
│       ├── Views/              # UI 뷰
│       └── Utils/              # 유틸리티
├── libs/                        # 라이브러리
│   ├── Jinobald.Core/          # 코어 프레임워크
│   ├── Jinobald.Avalonia/      # Avalonia UI 프레임워크
│   └── Jinobald.Wpf/           # WPF 프레임워크
├── tests/                       # 테스트 프로젝트
│   ├── Jinobald.Core.Tests/
│   ├── Jinobald.Avalonia.Tests/
│   └── Jinobald.Wpf.Tests/
└── docs/                        # 문서
    ├── CONDUCTOR_FEATURES.md
    ├── MULTI_REPO_IMPLEMENTATION.md
    ├── REFACTORING_PLAN.md
    └── WORK_SUMMARY.md
```

### 주요 구성요소

**src/Convalonia/** - 메인 애플리케이션
- **Models/**: Workspace, Agent, Message, Task 등 데이터 모델
- **Services/**: WorkspaceService, ClaudeCodeService, GitHubService 등 비즈니스 로직
- **ViewModels/**: MVVM 뷰모델 (MainWindow, WorkspaceList, Workspace, Chat 등)
- **Views/**: XAML UI 뷰 파일

**libs/** - Jinobald 프레임워크 라이브러리 소스 코드
- 현재는 NuGet 패키지(v1.0.1)를 사용하지만, 향후 수정이 필요할 경우를 대비한 소스

## 기술 스택

- **Framework**: .NET 9.0
- **UI**: Avalonia UI 11.3.9
- **MVVM**: Jinobald Framework + CommunityToolkit.Mvvm
- **Claude Integration**: Claude Code CLI (프로세스 통신)
- **Git**: System.Diagnostics.Process를 통한 Git 명령 실행

## 필수 요구사항

### 1. Claude Code CLI 설치

이 프로그램은 Claude Code CLI를 사용합니다. 설치가 필요합니다:

```bash
# Claude Code CLI 설치 방법은 공식 문서 참조
# https://github.com/anthropics/claude-code
```

Claude API 키는 Claude Code CLI가 자동으로 관리하므로 별도 설정이 필요없습니다.

## 설치 및 실행

### 1. 프로젝트 빌드

```bash
# 메인 애플리케이션 빌드
dotnet build src/Convalonia/Convalonia.csproj

# 또는 전체 솔루션 빌드 (Mac에서는 WPF 프로젝트 제외됨)
dotnet build Convalonia.slnx
```

### 2. 실행

```bash
dotnet run --project src/Convalonia/Convalonia.csproj
```

## 사용 방법

### 워크스페이스 생성

1. 메인 화면에서 "Workspaces" 탭으로 이동
2. 워크스페이스 이름 입력
3. (선택사항) Git 리포지토리 URL 입력
4. "Create Workspace" 버튼 클릭

### 에이전트 생성 및 사용

1. 워크스페이스를 선택하여 열기
2. "+ New Agent" 버튼으로 새 에이전트 생성
3. 에이전트를 선택하여 채팅 시작
4. 메시지를 입력하고 "Send" 버튼으로 Claude와 대화
5. 오른쪽 터미널 패널에서 Claude Code CLI의 실시간 출력 확인
6. "Toggle Terminal" 버튼으로 터미널 표시/숨김

### 워크스페이스 관리

- **Open Folder**: 워크스페이스 폴더를 파일 탐색기에서 열기
- **Stop Session**: 현재 Claude Code CLI 세션 종료
- **Stop All Agents**: 모든 실행 중인 에이전트 정지
- **Delete**: 워크스페이스 삭제

## 주요 클래스 설명

### Models

- **Workspace**: 워크스페이스 정보 (이름, 경로, Git 브랜치, 에이전트 목록)
- **Agent**: Claude 에이전트 정보 (이름, 상태, 메시지 기록, 모델)
- **Message**: 대화 메시지 (역할, 내용, 타임스탬프)
- **Task**: 에이전트가 수행하는 작업 정보

### Services

- **WorkspaceService**: 워크스페이스 생성/삭제/관리
- **ClaudeCodeService**: Claude Code CLI와 프로세스 통신 (stdin/stdout)
- **GitHubService**: Git 작업 (클론, 브랜치, 커밋, 푸시)
- **FileSystemService**: 파일 읽기/쓰기/검색

### ViewModels

- **WorkspaceListViewModel**: 워크스페이스 목록 관리
- **WorkspaceViewModel**: 단일 워크스페이스 + 에이전트 관리
- **ChatViewModel**: Claude와의 채팅 인터페이스

## 주요 특징

### 1. Claude Code CLI 통합
- 각 에이전트가 독립적인 `claude` 프로세스를 실행
- stdin으로 사용자 명령 전달, stdout/stderr로 출력 수신
- 환경변수 설정 불필요 (Claude Code CLI가 자동 관리)

### 2. 실시간 터미널 출력
- 왼쪽: 사용자 메시지 목록
- 오른쪽: Claude Code CLI의 실시간 터미널 출력
- 분할 뷰로 작업 과정을 실시간으로 확인

### 3. 병렬 작업 지원
- 여러 워크스페이스에서 동시에 작업 가능
- 각 워크스페이스는 독립적인 디렉토리와 Git 상태 유지
- 워크스페이스당 여러 에이전트 동시 실행 가능

## 향후 개선 사항

- [ ] 터미널 출력 ANSI 색상 지원
- [ ] 에이전트 간 작업 공유
- [ ] PR 생성 자동화
- [ ] 설정 UI (모델 선택, 워크스페이스 경로 등)
- [ ] 워크스페이스 템플릿
- [ ] 작업 기록 및 로그 저장
- [ ] 세션 복원 기능
- [ ] 다중 언어 지원

## 라이선스

MIT License

## 기여

이슈와 풀 리퀘스트를 환영합니다!
