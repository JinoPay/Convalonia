# Conductor 핵심 기능 분석 및 구현 가이드

> Convalonia에서 구현해야 할 Conductor의 모든 기능을 정리한 문서

---

## 📚 목차
1. [워크스페이스 시스템](#1-워크스페이스-시스템)
2. [Scripts (conductor.json)](#2-scripts-conductorjson)
3. [환경 변수](#3-환경-변수)
4. [병렬 에이전트](#4-병렬-에이전트)
5. [Checkpoints](#5-checkpoints)
6. [Diff Viewer](#6-diff-viewer)
7. [개발 워크플로우](#7-개발-워크플로우)

---

## 1. 워크스페이스 시스템

### 개념
- **워크스페이스** = Git 리포지토리의 독립된 복사본 + 브랜치
- 각 워크스페이스는 격리된 환경에서 작업
- Git에서 추적되는 파일만 자동 복사

### 핵심 원칙
```
1 워크스페이스 = 1 브랜치 = 1 기능/버그픽스
```

### 워크스페이스 생성 방법
1. **로컬 폴더에서**: 기존 Git 리포지토리 추가
2. **Git URL에서**: 리포지토리 클론
3. **특정 브랜치에서**: 기존 브랜치로부터 생성
4. **GitHub PR에서**: PR 체크아웃하여 생성
5. **Linear Issue에서**: 이슈로부터 생성

### 워크스페이스 식별
```
워크스페이스 이름 = 브랜치 이름
디렉토리 이름 = 랜덤 생성 (예: warsaw-v2, tokyo)
```

### 브랜치 제약사항
**중요**: 하나의 브랜치는 동시에 하나의 워크스페이스에서만 체크아웃 가능

**해결책**:
```bash
# 방법 1: 새 브랜치 생성
git checkout -b feature-branch-2 feature-branch

# 방법 2: 다른 워크스페이스를 다른 브랜치로 전환
# 워크스페이스 A에서:
git checkout -b dummy
# 그 후 워크스페이스 B에서:
git checkout feature-branch
```

### GitHub 인증 요구사항
```bash
# GitHub CLI 인증 필요
gh auth status

# 로그인 안 되어 있으면
gh auth login
```

### Convalonia 구현 체크리스트
- [ ] Git 파일만 복사하는 워크스페이스 생성
- [ ] 브랜치 기반 워크스페이스 생성
- [ ] GitHub PR에서 워크스페이스 생성
- [ ] Linear Issue 연동 (선택사항)
- [ ] 브랜치 중복 체크아웃 방지
- [ ] GitHub CLI 인증 확인

---

## 2. Scripts (conductor.json)

### conductor.json 파일 구조
```json
{
  "scripts": {
    "setup": "./conductor-setup.sh",
    "run": "npm run dev",
    "archive": "rm -rf \"$HOME/Library/Application Support/com.conductor.app.dev.$CONDUCTOR_WORKSPACE_NAME\""
  },
  "runScriptMode": "nonconcurrent"
}
```

### 2.1 Setup Script

**목적**: 워크스페이스 생성 시 자동 실행

**실행 시점**: 새 워크스페이스 디렉토리 생성 직후

**실행 위치**: `$CONDUCTOR_WORKSPACE_PATH`

**일반적인 사용 사례**:
```bash
#!/bin/bash
# conductor-setup.sh

# 1. 의존성 설치
npm install

# 2. .env 파일 복사
cp "$CONDUCTOR_ROOT_PATH/.env" .env

# 3. 앱 빌드
npm run build

# 4. 데이터베이스 마이그레이션
npm run db:migrate

# 5. Symlink (공유 파일)
ln -s "$CONDUCTOR_ROOT_PATH/shared-config.json" config.json
```

**실제 Conductor 팀 예제**:
- [Conductor 자체 setup script](https://github.com/conductor-is/conductor/blob/main/conductor-setup.sh)

### 2.2 Run Script

**목적**: 개발 서버/앱/테스트 실행

**실행 방법**: UI의 "Run" 버튼 클릭

**실행 위치**: `$CONDUCTOR_WORKSPACE_PATH`

**예제**:
```json
{
  "scripts": {
    "run": "python3 -m http.server --port $CONDUCTOR_PORT"
  }
}
```

```json
{
  "scripts": {
    "run": "npm run dev -- --port $CONDUCTOR_PORT"
  }
}
```

```json
{
  "scripts": {
    "run": "bundle exec rails server -p $CONDUCTOR_PORT"
  }
}
```

### 2.3 Archive Script

**목적**: 워크스페이스 삭제 시 정리 작업

**실행 시점**: 워크스페이스 아카이브/삭제 시

**사용 사례**:
```bash
# 외부 리소스 정리
rm -rf "$HOME/Library/Application Support/com.conductor.app.dev.$CONDUCTOR_WORKSPACE_NAME"

# 임시 파일 정리
rm -rf /tmp/workspace-$CONDUCTOR_WORKSPACE_NAME-*

# 데이터베이스 정리
npm run db:drop
```

### 2.4 Run Script Mode

**nonconcurrent 모드**: 여러 dev 서버 동시 실행 불가능할 때

```json
{
  "scripts": {
    "run": "npm run dev"
  },
  "runScriptMode": "nonconcurrent"
}
```

**동작**:
- Run 버튼 클릭 시 기존 실행 중인 스크립트 자동 종료
- 새 스크립트 시작
- 포트 충돌 방지

**기본값**: concurrent (여러 워크스페이스에서 동시 실행 가능)

### 스크립트 실행 환경
- **Shell**: zshell
- **환경 변수**: Conductor 환경 변수 모두 사용 가능
- **에러 처리**: 스크립트 실패 시 UI에 에러 표시

### Convalonia 구현 체크리스트
- [ ] conductor.json 파싱
- [ ] Setup script 실행 (워크스페이스 생성 시)
- [ ] Run script 실행 (Run 버튼)
- [ ] Archive script 실행 (워크스페이스 삭제 시)
- [ ] nonconcurrent 모드 지원
- [ ] 스크립트 출력을 터미널에 표시
- [ ] 스크립트 에러 핸들링

---

## 3. 환경 변수

### Conductor 제공 환경 변수

#### `$CONDUCTOR_WORKSPACE_PATH`
```bash
# 현재 워크스페이스의 절대 경로
/Users/username/.conductor/workspaces/warsaw-v2
```

**사용 예**:
```bash
cd "$CONDUCTOR_WORKSPACE_PATH"
npm install
```

#### `$CONDUCTOR_ROOT_PATH`
```bash
# 리포지토리 루트 디렉토리 (워크스페이스 간 공유)
/Users/username/.conductor/repositories/my-repo
```

**사용 예**:
```bash
# .env 파일 복사
cp "$CONDUCTOR_ROOT_PATH/.env" .env

# 설정 파일 symlink
ln -s "$CONDUCTOR_ROOT_PATH/config" config
```

**용도**:
- 워크스페이스 간 공유 파일 저장
- .env, 인증서, 설정 파일 등

#### `$CONDUCTOR_PORT`
```bash
# 워크스페이스에 할당된 포트
3000
```

**포트 범위**: `$CONDUCTOR_PORT` ~ `$CONDUCTOR_PORT+9` (총 10개)

**사용 예**:
```bash
# 웹 서버
python3 -m http.server --port $CONDUCTOR_PORT

# React 앱
npm run dev -- --port $CONDUCTOR_PORT

# Rails
bundle exec rails server -p $CONDUCTOR_PORT

# 추가 서비스 (포트 +1, +2, ...)
redis-server --port $((CONDUCTOR_PORT + 1))
postgres -p $((CONDUCTOR_PORT + 2))
```

#### `$CONDUCTOR_WORKSPACE_NAME`
```bash
# 워크스페이스 이름
feature-auth-system
```

**사용 예**:
```bash
# 로그 파일
echo "Starting workspace: $CONDUCTOR_WORKSPACE_NAME"

# 리소스 정리
rm -rf "/tmp/$CONDUCTOR_WORKSPACE_NAME-cache"
```

### Convalonia 구현 체크리스트
- [ ] 환경 변수 설정 시스템
- [ ] WORKSPACE_PATH 생성 및 전달
- [ ] ROOT_PATH 관리 (리포지토리별)
- [ ] PORT 자동 할당 (워크스페이스당 10개)
- [ ] WORKSPACE_NAME 생성 (브랜치명 기반)
- [ ] 스크립트 실행 시 환경 변수 주입

---

## 4. 병렬 에이전트

### 개념
```
여러 Claude Code를 동시에 실행
각 워크스페이스 = 독립적인 Claude 인스턴스
```

### 핵심 기능
1. **격리성**: 각 워크스페이스의 변경사항은 다른 워크스페이스에 영향 없음
2. **독립성**: 각 Claude는 자체 컨텍스트, 메모리, 작업 공간 보유
3. **병렬성**: 동시에 여러 기능 개발 가능

### 생성 방법
```
⌘ + N → 새 워크스페이스 생성
```

### 사용 시나리오
```
워크스페이스 A: feature-auth (Claude가 인증 시스템 개발)
워크스페이스 B: feature-payments (Claude가 결제 시스템 개발)
워크스페이스 C: bugfix-login (Claude가 로그인 버그 수정)
```

### Convalonia 구현 체크리스트
- [x] 여러 워크스페이스 동시 관리
- [x] 각 워크스페이스에 독립적인 Claude Code 프로세스
- [ ] ⌘N 단축키로 새 워크스페이스 생성
- [x] 워크스페이스 간 격리 보장

---

## 5. Checkpoints

### 개념
```
Claude의 변경사항을 턴별로 자동 스냅샷
이전 턴으로 되돌리기 가능 (영구 삭제)
```

### 동작 원리

#### 5.1 자동 스냅샷
```
사용자 메시지 전송 → Conductor hook → 현재 상태 커밋 → Private Git ref
```

**Git Ref 구조**:
```
refs/conductor/checkpoints/{workspace-id}/turn-{number}
```

**예시**:
```
refs/conductor/checkpoints/warsaw-v2/turn-1
refs/conductor/checkpoints/warsaw-v2/turn-2
refs/conductor/checkpoints/warsaw-v2/turn-3
```

#### 5.2 저장 위치
- **로컬 저장**: 워크스페이스 로컬 Git 리포지토리
- **Working branch와 분리**: Private ref 사용
- **Git history에 영향 없음**: 일반 커밋과 별개

#### 5.3 Checkpoint 구성 요소
```
Checkpoint {
  - Git commit SHA
  - Turn number
  - User message
  - Assistant message (response)
  - Code changes (diff)
  - Timestamp
}
```

### Revert (되돌리기)

#### UI
```
메시지에 마우스 호버 → Revert 아이콘 표시 → 클릭
```

#### 동작
```
1. Git reset --hard {checkpoint-sha}
2. 선택한 턴 이후 모든 메시지 삭제 (영구 삭제)
3. 코드 변경사항 완전 되돌리기
4. Claude는 삭제된 대화 내용 모름
```

**경고**: 되돌리기는 영구적이며 취소 불가능

### 주의사항
```
⚠️ 여러 채팅이 같은 워크스페이스에서 실행 중일 때 주의
⚠️ Checkpoints는 Claude Code만 지원
```

### Checkpoint vs Git Commit
| 구분 | Checkpoint | Git Commit |
|------|-----------|-----------|
| 생성 시점 | 각 턴마다 자동 | 수동 또는 Claude가 명령 |
| 저장 위치 | Private ref | Working branch |
| 되돌리기 | 메시지도 함께 삭제 | 코드만 되돌리기 |
| 영속성 | 로컬만 | Push 가능 |

### Convalonia 구현 체크리스트
- [ ] 턴별 자동 스냅샷 생성
- [ ] Private Git ref 생성 및 관리
- [ ] Checkpoint 메타데이터 저장 (JSON)
- [ ] Revert UI (메시지 호버 시 아이콘)
- [ ] Revert 확인 다이얼로그
- [ ] Git reset --hard 실행
- [ ] 메시지 삭제 (턴 이후 모두)
- [ ] Checkpoint 목록 표시

---

## 6. Diff Viewer

### 개념
```
Claude가 만든 코드 변경사항을 시각적으로 확인
GitHub와 동기화
PR 생성 워크플로우 지원
```

### 단축키
```
⌘ + D → Diff Viewer 열기
```

### 기능

#### 6.1 변경사항 확인
```
- 파일별 diff 표시
- 추가/삭제/수정 라인 하이라이트
- 원본 vs 변경본 나란히 비교
```

#### 6.2 GitHub 동기화
```
1. 로컬 변경사항 확인
2. Commit (자동 또는 수동)
3. Push to remote
4. GitHub에서 확인
```

#### 6.3 PR 생성 추천
```
Conductor가 다음 단계 추천:
1. Review changes (Diff Viewer)
2. Run tests
3. Commit
4. Push
5. Create PR (⌘⇧P)
```

### PR 생성 워크플로우

#### 단축키
```
⌘ + Shift + P → Pull Request 생성
```

#### 자동화
```
1. 브랜치 push (아직 안 했으면)
2. PR 제목 자동 생성 (브랜치명 기반)
3. PR 본문 자동 생성 (커밋 메시지 기반)
4. GitHub PR 생성 (gh CLI 사용)
5. PR URL 표시
```

#### PR 체크 실패 시
```
Conductor가 자동으로:
1. 실패한 체크 확인
2. 에러 분석
3. 수정 제안
4. 수정 후 재시도
```

### Convalonia 구현 체크리스트
- [ ] Git diff 파싱 및 표시
- [ ] Diff Viewer UI (파일 목록 + diff)
- [ ] ⌘D 단축키
- [ ] Syntax highlighting
- [ ] GitHub push 기능
- [ ] PR 생성 (gh CLI 연동)
- [ ] ⌘⇧P 단축키
- [ ] PR 체크 모니터링
- [ ] 실패 시 수정 제안

---

## 7. 개발 워크플로우

### Conductor 권장 워크플로우

```
1. Create Workspace (per feature)
   ↓
2. Develop (Claude Code or IDE)
   ↓
3. Review & Test (⌘D Diff Viewer, Terminal/Run panel)
   ↓
4. Create PR (⌘⇧P)
   ↓
5. Merge
   ↓
6. Archive Workspace
```

### 각 단계 상세

#### Step 1: Create Workspace
```
- ⌘⇧N or ••• 버튼
- 브랜치/PR/Issue에서 생성 가능
- 1 feature = 1 workspace
```

#### Step 2: Develop
```
옵션 1: Conductor 내장 Claude Code 사용
옵션 2: ⌘O로 IDE에서 작업
```

#### Step 3: Review & Test
```
- ⌘D: Diff Viewer로 변경사항 확인
- Terminal: 명령어 직접 실행
- Run panel: npm run dev 등 실행
```

**서버 실행 가이드**: [Running a workspace](https://docs.conductor.build/guides/how-to-run)

#### Step 4: Create PR & Merge
```
- ⌘⇧P: PR 생성
- 체크 실패 시 Conductor가 수정 도움
- 모든 체크 통과 시 머지
```

#### Step 5: Archive
```
- 작업 완료 후 워크스페이스 아카이브
- 채팅 히스토리 보존
- 언제든지 복원 가능
```

### 워크스페이스 생애주기

```
[생성] → [활성] → [작업 중] → [리뷰] → [PR 생성] → [머지됨] → [아카이브]
   ↑                                                              ↓
   └──────────────────────────────────────────────────────────────┘
                           (필요 시 복원)
```

### 단축키 요약

| 단축키 | 기능 |
|--------|------|
| ⌘N | 새 워크스페이스 생성 |
| ⌘⇧N | 워크스페이스 생성 옵션 (브랜치/PR/Issue) |
| ⌘O | IDE에서 열기 |
| ⌘D | Diff Viewer |
| ⌘⇧P | Pull Request 생성 |

### Convalonia 구현 체크리스트
- [ ] 워크스페이스 생성 UI (단축키 포함)
- [ ] IDE에서 열기 기능 (⌘O)
- [ ] Diff Viewer (⌘D)
- [ ] PR 생성 (⌘⇧P)
- [ ] 워크스페이스 아카이브/복원
- [ ] 채팅 히스토리 영속성

---

## 8. 기타 기능

### MCP (Model Context Protocol)
```
외부 도구 및 데이터 소스 연결
```
- Convalonia에서는 추후 구현

### Slash Commands
```
채팅 내에서 커스텀 명령어 실행
```
- 예: `/review-code`, `/run-tests`
- Convalonia에서는 추후 구현

### Configuration
```
conductor.json에 추가 설정
```
- `runScriptMode`: "nonconcurrent"
- 향후 확장 가능 (모델 설정, 플러그인 등)

---

## 9. 프레임워크별 예제

### Next.js + Vercel
```json
{
  "scripts": {
    "setup": "npm install",
    "run": "npm run dev -- --port $CONDUCTOR_PORT"
  }
}
```

### Rails
```json
{
  "scripts": {
    "setup": "bundle install && rails db:setup",
    "run": "bundle exec rails server -p $CONDUCTOR_PORT"
  },
  "runScriptMode": "nonconcurrent"
}
```

### Django
```json
{
  "scripts": {
    "setup": "pip install -r requirements.txt && python manage.py migrate",
    "run": "python manage.py runserver $CONDUCTOR_PORT"
  }
}
```

### Elixir + Phoenix
```json
{
  "scripts": {
    "setup": "mix deps.get && mix ecto.setup",
    "run": "mix phx.server"
  }
}
```

---

## 10. 트러블슈팅

### 워크스페이스 관련
**문제**: 브랜치가 이미 다른 워크스페이스에서 체크아웃됨
**해결**:
```bash
# 새 브랜치 생성
git checkout -b feature-branch-2 feature-branch
```

### 환경 변수 관련
**문제**: .env 파일이 워크스페이스에 없음
**해결**:
```bash
# setup script에 추가
cp "$CONDUCTOR_ROOT_PATH/.env" .env
```

### API 키 관련
**문제**: Claude API 키 설정
**해결**: Claude Code CLI가 자동 관리 (별도 설정 불필요)

### 포트 충돌
**문제**: 포트가 이미 사용 중
**해결**: Conductor가 자동 할당하는 `$CONDUCTOR_PORT` 사용

---

## 11. Convalonia 우선순위 구현 목록

### Phase 1: 필수 기능 (MVP)
1. ✅ 워크스페이스 생성/관리
2. ✅ Git 연동 (clone, branch, commit)
3. ✅ Claude Code 프로세스 통합
4. ✅ 채팅 UI
5. [ ] conductor.json 파싱 및 실행
6. [ ] 환경 변수 시스템
7. [ ] Run 버튼 (run script 실행)

### Phase 2: 핵심 기능
8. [ ] Checkpoints (턴별 스냅샷)
9. [ ] Diff Viewer
10. [ ] PR 생성 자동화
11. [ ] 워크스페이스 아카이브/복원
12. [ ] Setup script 실행

### Phase 3: 고급 기능
13. [ ] Slash commands
14. [ ] MCP 연동
15. [ ] 워크스페이스 템플릿
16. [ ] 병렬 에이전트 최적화

---

## 12. 참고 자료

### 공식 문서
- [Conductor Docs](https://docs.conductor.build)
- [First Workspace](https://docs.conductor.build/first-workspace)
- [Workflow](https://docs.conductor.build/workflow)
- [Scripts](https://docs.conductor.build/core/scripts)
- [Checkpoints](https://docs.conductor.build/core/checkpoints)
- [Parallel Agents](https://docs.conductor.build/core/parallel-agents)
- [Setting up a workspace](https://docs.conductor.build/guides/how-to-setup)
- [Running a workspace](https://docs.conductor.build/guides/how-to-run)

### 예제 리포지토리
- [Conductor 자체 setup script](https://github.com/conductor-is/conductor)

---

**문서 버전**: 1.0
**마지막 업데이트**: 2025-12-07
**작성자**: Convalonia 팀
