# 코드 리뷰

## Checklist
- [x] 리뷰 범위와 현재 작업 트리 상태를 확인한다
- [x] 핵심 실행 흐름과 UI 이벤트 처리를 검토한다
- [x] Win32 interop, 프로세스 실행, 설정 저장 경로의 위험을 확인한다
- [x] 가능한 빌드 또는 정적 검증을 실행한다
- [x] 발견 사항, 검증 결과, 잔여 위험을 기록한다

## Review
- 발견 1: `AppLauncherService`는 `.lnk`, URL, 문서 등 `UseShellExecute=true` 실행 대상을 항상 창 위치 제어 불가로 처리한다. README와 파일 선택 필터가 `.lnk` 실행을 지원 기능처럼 노출하므로, 사용자는 바로가기 프로필이 선택 모니터로 이동할 것으로 기대하지만 실제로는 실행만 되고 위치 제어는 건너뛴다.
- 발견 2: 프로필 저장 시 기존 프로필 제거와 새 프로필 추가를 디스크 저장 전에 `profiles` 리스트에 먼저 반영한다. 저장 실패가 발생하면 메모리 상태와 디스크/UI 상태가 어긋나고, 이후 저장 성공 시 실패했던 변경이 뒤늦게 디스크에 반영될 수 있다.
- 발견 3: `cmbMonitors.SelectedIndex`가 현재 `Screen.AllScreens` 길이보다 작은지 확인하지 않고 인덱싱하는 경로가 있다. 모니터 연결 상태가 드롭다운 생성 후 바뀌면 실행 또는 저장 시 예외가 발생할 수 있다.
- 검증: `rg`로 핵심 위험 패턴을 확인하고 주요 C# 파일을 정적으로 검토했다.
- 제한: 현재 환경에 `dotnet` 명령이 없어 `dotnet build MonitorLauncher.csproj -c Release`는 실행하지 못했다.

# 모니터 인덱스 검증 보강

## Checklist
- [x] `MainForm`의 실행/저장 경로에서 `Screen.AllScreens` 인덱싱 위치를 확인한다
- [x] 실행 전 현재 선택된 모니터 인덱스가 최신 화면 목록에 유효한지 검증한다
- [x] 저장 전 현재 선택된 모니터 인덱스가 최신 화면 목록에 유효한지 검증한다
- [x] 모니터 연결 상태 변경 시 사용자에게 새로고침/재선택 안내 메시지를 보여준다
- [x] 프로필 저장 실패 시 메모리 상태가 먼저 바뀌지 않도록 저장 순서를 안전하게 만든다
- [x] 가능한 빌드 또는 정적 검증을 실행한다
- [x] 결과와 사용자 메시지를 기록한다

## Review
- `TryGetSelectedMonitor()`를 추가해 실행과 프로필 저장 모두 `Screen.AllScreens[cmbMonitors.SelectedIndex]` 접근 전에 선택 인덱스가 현재 화면 목록 범위 안인지 확인한다.
- 모니터 연결 상태가 바뀌어 선택 인덱스가 무효해지면 "모니터 연결 상태가 변경되어 선택한 모니터를 실행/프로필 저장에 사용할 수 없습니다. 모니터 목록을 새로고침한 뒤 다시 선택해주세요." 메시지를 경고로 보여주고 상태 표시줄에도 남긴다.
- 프로필 저장은 새 목록을 만든 뒤 파일 저장이 성공했을 때만 `profiles` 필드를 교체하도록 바꿔, 저장 실패 후 메모리/UI 상태가 디스크와 어긋나는 일을 막았다.
- 검증: `rg -n "Screen\\.AllScreens\\[|TryGetSelectedMonitor|SaveProfiles\\(" MainForm.cs Profile.cs`로 직접 인덱싱 위치와 저장 호출을 확인했다.
- 제한: 현재 환경에 `dotnet` 명령이 없어 `dotnet build MonitorLauncher.csproj -c Release`는 실행하지 못했다.

# v1.2.6 모니터 안전성 개선

## Checklist
- [x] `codex/monitor-safety-v1.2.6` 브랜치를 생성한다
- [x] 콤보박스 모니터 항목을 현재 화면 스냅샷 객체로 관리한다
- [x] 실행/저장 시 선택된 스냅샷을 현재 화면 목록과 재매칭해 잘못된 모니터 실행을 막는다
- [x] 모니터 변경 안내 메시지를 실제 동작과 맞게 정리한다
- [x] 프로필 삭제도 저장 성공 후 메모리 상태를 교체하도록 통일한다
- [x] 앱/어셈블리/문서/체인지로그 버전을 1.2.6으로 올린다
- [x] 가능한 빌드 또는 정적 검증을 실행한다
- [x] 결과를 기록한다

## Review
- 새 브랜치 `codex/monitor-safety-v1.2.6`에서 작업을 진행했다.
- `MonitorOption` 스냅샷을 `MainForm` 내부 private 타입으로 추가해 콤보박스 선택이 현재 화면 목록과 동일한 장치명/좌표/크기/기본 여부를 가진 화면에만 매칭되도록 했다.
- 실행/프로필 저장 전 선택된 모니터 스냅샷을 현재 `Screen.AllScreens`와 재검증한다. 매칭 실패 시 "모니터 연결 상태가 변경되어 선택한 모니터를 실행/프로필 저장에 사용할 수 없습니다. 모니터 목록을 새로고침했습니다. 사용할 모니터를 다시 선택해주세요."를 표시한다.
- 프로필 저장과 삭제 모두 새 목록을 만든 뒤 파일 저장 성공 후 `profiles`를 교체하도록 통일했다.
- `MainForm`, `MonitorLauncher.csproj`, `app.manifest`, `README.md`, `CHANGELOG.md` 버전을 1.2.6으로 올렸다.
- 검증: `rg -n "Screen\\.AllScreens\\[|Monitor Launcher v1\\.2\\.5|1\\.2\\.5\\.0|<Version>1\\.2\\.5" -S`로 직접 인덱싱과 주요 구버전 표기 누락을 확인했다. 결과는 이전 작업 기록의 설명 문장과 README의 과거 v1.2.5 변경 이력만 남았다.
- 제한: 현재 환경에 `dotnet` 명령이 없어 `dotnet build MonitorLauncher.csproj -c Release`는 실행하지 못했다.

# 문서 최신화 확인

## Checklist
- [x] 문서/설정 파일의 버전 문자열을 검색한다
- [x] README와 CHANGELOG의 최신 버전 항목을 확인한다
- [x] README 프로젝트 구조가 현재 저장소 파일과 맞는지 정리한다
- [x] 문서 최신화 결과를 기록한다

## Review
- `rg`로 `README.md`, `CHANGELOG.md`, `tasks/todo.md`, `MonitorLauncher.csproj`, `app.manifest`, `MainForm.cs`의 버전 문자열을 확인했다.
- 최신 버전 표기는 `README.md`, `CHANGELOG.md`, `MonitorLauncher.csproj`, `app.manifest`, `MainForm.cs` 모두 1.2.6으로 맞춰져 있다.
- README의 프로젝트 구조에 `AppLauncherService.cs`, `LaunchRequest.cs`, `LaunchResult.cs`, `AppWindowState.cs`, `tasks/`, `CHANGELOG.md`를 반영했다.
- 현재 저장소에 없는 `docs/` 항목을 README 구조에서 제거했다.
- 실제 `LICENSE` 파일이 없어 깨져 있던 라이선스 배지 링크를 일반 배지로 변경했다.
- `v1.2.5`는 README/CHANGELOG의 과거 변경 이력과 작업 기록 안에서만 남아 있으며 최신 버전 표기 누락은 아니다.

# 코드 리뷰

## Checklist
- [x] 현재 브랜치와 변경 범위를 확인한다
- [x] `MainForm`의 모니터 새로고침/실행/저장 경로를 검토한다
- [x] 프로필 저장/삭제 경로를 검토한다
- [x] 가능한 빌드 또는 정적 검증을 실행한다
- [x] 발견 사항과 잔여 위험을 기록한다

## Review
- 발견 1: 모니터 매칭 실패 시 `TryGetSelectedMonitor()`가 `RefreshMonitorList()`를 호출하고, `RefreshMonitorList()`는 복원할 선택이 없으면 기본 모니터를 자동 선택한다. 사용자는 "다시 선택해주세요" 메시지를 보지만, 실제 UI에는 이미 기본 모니터가 선택되어 있어 같은 버튼을 다시 누르면 기본 모니터로 실행/저장될 수 있다.
- 발견 2: `MonitorOption.Matches()`가 `IsPrimary`까지 동일해야 같은 모니터로 본다. 같은 물리 모니터에서 기본 모니터 여부만 바뀐 경우에도 현재 선택이 무효 처리되어 불필요한 재선택 안내가 발생할 수 있다.
- 검증: `rg`로 선택 인덱스, 저장 호출, 모니터 매칭 경로를 확인했다.
- 제한: 현재 환경에 `dotnet` 명령이 없어 `dotnet build MonitorLauncher.csproj -c Release`는 실행하지 못했다.

# v2.0 워크스페이스 매니저 MVP

## Checklist
- [x] 기존 창 제어/프로필 저장 구조를 확인한다
- [x] `WorkspaceProfile`, `AppWindowProfile`, 캡처용 창 모델을 추가한다
- [x] `WindowCaptureService`로 현재 일반 창 목록을 수집한다
- [x] `WorkspaceRestoreService`로 저장된 창 실행/이동/주 모니터 fallback을 구현한다
- [x] 워크스페이스 JSON 저장/로드를 기존 프로필 방식과 맞춘다
- [x] `MainForm`에 워크스페이스 탭과 저장/실행/삭제/모으기 UI를 추가한다
- [x] 트레이 메뉴에 워크스페이스 빠른 실행을 추가한다
- [x] 기존 단일 앱 실행 경로가 유지되는지 정적 검증한다
- [x] 가능한 빌드 또는 대체 검증을 실행하고 결과를 기록한다

## Review
- `WorkspaceProfile`, `AppWindowProfile`, `CapturedWindowInfo`, `WorkspaceRestoreResult` 모델을 추가했다.
- `WorkspaceProfile`은 기존 `Profile`과 같은 방식으로 `%APPDATA%\MonitorLauncher\workspaces.json`에 JSON 저장/로드한다.
- `WindowCaptureService`는 `EnumWindows` 기반으로 visible window를 수집하고, 빈 제목/툴윈도우/작은 창/일부 시스템성 프로세스를 제외한다.
- `WorkspaceRestoreService`는 실행 중인 프로세스 창을 찾아 저장 좌표로 이동하고, 없으면 `LaunchIfNotRunning` 설정에 따라 실행 후 창을 감지해 이동한다.
- 저장 당시 모니터가 없으면 주 모니터 작업 영역 중앙으로 안전하게 이동한다.
- `MainForm`에 워크스페이스 탭을 추가해 현재 배치 가져오기, 선택 저장, 실행, 삭제, 모든 창 주 모니터로 모으기를 연결했다.
- 트레이 메뉴에 `워크스페이스 실행` 하위 메뉴를 추가했다.
- v2.0.0 버전을 `MainForm`, `MonitorLauncher.csproj`, `app.manifest`, `README.md`, `CHANGELOG.md`에 반영했다.
- 이전 리뷰에서 발견한 모니터 재선택 문제는 실패 후 기본 모니터 자동 선택을 하지 않도록 `RefreshMonitorList(selectDefaultWhenUnmatched: false)` 경로를 추가해 보완했다.
- 이전 리뷰에서 발견한 `MonitorOption.Matches()`의 기본 모니터 여부 과민 매칭은 장치명과 bounds 기준으로 완화했다.
- 검증: `git diff --check`, `rg` 기반 참조/버전/직접 인덱싱 검색을 실행했다.
- 제한: 현재 환경에 `dotnet` 명령이 없어 `dotnet build MonitorLauncher.csproj -c Release`는 실행하지 못했다.

# CHANGELOG 로컬 전용 처리

## Checklist
- [x] `CHANGELOG.md` 추적 상태를 확인한다
- [x] `.gitignore`에 `CHANGELOG.md`를 추가한다
- [x] 로컬 파일은 유지하고 Git 인덱스에서만 제거한다
- [x] 로컬 파일 존재와 ignore 적용을 검증한다

## Review
- `CHANGELOG.md`는 로컬 파일로 남겨두고 `git rm --cached CHANGELOG.md`로 Git 추적 대상에서 제거했다.
- `.gitignore`에 `CHANGELOG.md`를 추가해 이후 다시 untracked로 잡히지 않게 했다.
- 검증: `ls -l CHANGELOG.md`로 로컬 파일 존재를 확인했고, `git check-ignore -v CHANGELOG.md`로 ignore 적용을 확인했다.

# 변경사항 일관성 확인

## Checklist
- [x] 현재 Git 상태와 추적 파일 목록을 확인한다
- [x] v2.0.0 버전 표기와 워크스페이스 참조를 검색한다
- [x] `CHANGELOG.md` 로컬 전용 정책과 충돌하는 추적 문서 참조를 확인한다
- [x] README에서 Git에 포함되지 않는 `CHANGELOG.md` 안내를 제거한다
- [x] 정적 검증 결과를 기록한다

## Review
- `README.md`, 코드, 프로젝트 설정의 최신 버전 표기는 `2.0.0`으로 맞춰져 있다.
- 워크스페이스 모델/서비스/UI/트레이 메뉴 참조는 `README.md`, `MainForm.cs`, `WorkspaceProfile.cs`, `WindowCaptureService.cs`, `WorkspaceRestoreService.cs`에 반영되어 있다.
- `CHANGELOG.md`는 `.gitignore`에만 참조가 남아 있고, Git에 포함될 README 구조/변경 이력 링크에서는 제거했다.
- 검증: `rg`로 구버전 `Monitor Launcher v1.2.6`, `1.2.6.0`, `<Version>1.2.6`, 직접 `Screen.AllScreens[` 접근, `throw new NotImplemented`, 추적 문서의 `CHANGELOG.md` 참조를 확인했다.
- 검증: `git diff --check` 통과.

# 파비콘 적용 확인

## Checklist
- [x] `Resources/favicon.ico` 파일 상태를 확인한다
- [x] 프로젝트 파일의 앱 아이콘 설정을 확인한다
- [x] 폼 아이콘과 트레이 아이콘 적용 경로를 확인한다
- [x] 누락된 favicon 적용을 코드/프로젝트 설정에 반영한다
- [x] README 리소스 구조를 favicon 적용 상태에 맞춘다
- [x] 정적 검증 결과를 기록한다

## Review
- 기존 상태에서는 `Resources/favicon.ico` 파일은 있었지만 `MonitorLauncher.csproj`의 `ApplicationIcon`, `MainForm.Icon`, `NotifyIcon.Icon`에 연결되어 있지 않았다.
- `MonitorLauncher.csproj`에 `<ApplicationIcon>Resources\favicon.ico</ApplicationIcon>`을 추가했고, 빌드 출력에 `Resources\favicon.ico`가 복사되도록 설정했다.
- `MainForm` 시작 시 `this.Icon = LoadApplicationIcon()`으로 폼 아이콘을 적용하고, 트레이 아이콘도 같은 favicon을 사용하도록 바꿨다.
- `README.md`의 리소스 구조에 `favicon.ico`를 앱/폼/트레이 아이콘으로 반영했다.
- 검증: `rg`로 `ApplicationIcon`, `LoadApplicationIcon`, `this.Icon`, `NotifyIcon.Icon`, `favicon.ico` 참조를 확인했다.
- 검증: `file Resources/favicon.ico` 결과 Windows icon resource이며 현재 포함 이미지는 16x16 32bpp 1개다. 고해상도 작업표시줄/Alt-Tab 품질까지 보장하려면 32/48/256 크기를 포함한 멀티 사이즈 `.ico`로 교체하는 후속 개선이 필요하다.
- 제한: 현재 환경에 `dotnet` 명령이 없어 실제 빌드 산출물 아이콘 검증은 실행하지 못했다.
