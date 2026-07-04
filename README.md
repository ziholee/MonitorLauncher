# Monitor Launcher v2.0.0

<div align="center">
<img src="Resources/logo.png" width="40%" />

**Windows 다중 모니터 환경에서 앱 실행과 워크스페이스 창 배치를 복원하는 경량 유틸리티**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?logo=windows)](https://www.microsoft.com/windows)
![License](https://img.shields.io/badge/License-Free-green)

</div>

## 📖 프로젝트 소개

**Monitor Launcher**는 다중 모니터 환경에서 작업하는 사용자를 위한 Windows 유틸리티입니다. 특정 프로그램을 원하는 모니터에서 원하는 창 상태로 실행할 수 있도록 도와줍니다.

### 왜 필요한가요?

- 여러 모니터를 사용하지만, 프로그램이 항상 기본 모니터에서만 실행되는 문제
- 특정 작업용 프로그램을 항상 특정 모니터에서 실행하고 싶은 경우
- 게임, 개발 도구, 미디어 플레이어 등을 각각 다른 모니터에 배치하고 싶을 때
- 프로필을 저장하여 자주 사용하는 프로그램을 원클릭으로 실행하고 싶을 때

### 주요 특징

- 🖥️ **자동 모니터 감지**: 연결된 모든 모니터를 자동으로 감지하고 해상도 정보 표시
- 🎯 **정확한 창 위치 제어**: Win32 API를 활용한 강력한 창 위치 제어
- ⚡ **경량 및 고성능**: CPU 사용량 최적화 (0.5-3%), 메모리 사용량 25-35MB
- 💾 **프로필 저장**: 자주 사용하는 프로그램 설정을 프로필로 저장하여 빠른 실행
- 🔄 **지속적인 모니터링**: 프로그램이 창 위치를 변경해도 자동으로 원하는 모니터로 재배치
- 🎨 **직관적인 UI**: 간단하고 깔끔한 Windows Forms 인터페이스
- 🔔 **시스템 트레이 지원**: 백그라운드 실행 및 트레이 아이콘을 통한 빠른 접근



## ✨ 주요 기능

- **모니터 감지 및 선택**: 연결된 모든 모니터를 자동 감지하고 선택할 수 있습니다.
- **프로그램 실행 위치 제어**: 실행 파일을 선택한 모니터에서 실행할 수 있습니다.
- **프로필(즐겨찾기) 기능**: 프로그램 경로, 모니터, 창 상태를 프로필로 저장하여 원클릭으로 실행할 수 있습니다.
- **워크스페이스 기능**: 여러 앱 창의 위치와 크기를 하나의 워크스페이스로 저장하고 복원할 수 있습니다.
- **창 상태 제어**: 전체화면, 창모드, 복원 옵션을 지원합니다.
- **시스템 트레이 지원**: 백그라운드 실행 및 트레이 아이콘을 통한 빠른 접근
- **자동 창 위치 모니터링**: 프로그램이 창 위치를 변경해도 자동으로 원하는 모니터로 재배치
- **유연한 실행 대상 지원**: `.exe`, `.lnk`, URL/셸 실행 대상을 처리할 수 있습니다.

## 주요 파일 구조

~~~
MonitorLauncher/
├── MonitorLauncher.csproj  # 프로젝트 파일
├── app.manifest            # 관리자 권한 요구 설정
├── Program.cs              # 진입점
├── MainForm.cs             # 메인 UI 및 로직
├── AppLauncherService.cs   # 프로그램 실행 및 창 이동 조정
├── WorkspaceRestoreService.cs # 워크스페이스 복원 및 창 모으기
├── WindowCaptureService.cs # 현재 열려 있는 창 캡처
├── LaunchRequest.cs        # 실행 요청 모델
├── LaunchResult.cs         # 실행 결과 모델
├── WorkspaceProfile.cs     # 워크스페이스 저장/로드
├── AppWindowProfile.cs     # 워크스페이스 앱 창 모델
├── CapturedWindowInfo.cs   # 캡처된 창 정보
├── AppWindowState.cs       # 창 상태 enum
├── Win32Api.cs             # Win32 API 래퍼
├── WindowController.cs     # 창 위치 제어 로직
├── Profile.cs              # 프로필 저장/로드
├── Resources/              # 앱 로고, 앱 아이콘, 트레이 아이콘 리소스
├── tasks/                  # 작업 기록
└── README.md               # 이 파일
~~~

## 🚀 빠른 시작

### 요구사항

- Windows 10 또는 Windows 11
- .NET 8.0 Runtime ([다운로드](https://dotnet.microsoft.com/download/dotnet/8.0))
- 관리자 권한 (일부 프로그램 제어를 위해 권장)

### 설치 방법

#### 방법 1: GitHub Releases에서 다운로드 (권장)

1. [Releases 페이지](https://github.com/your-username/MonitorLauncher/releases)에서 최신 버전 다운로드
2. `MonitorLauncher.exe` 실행
3. 관리자 권한으로 실행 권장

#### 방법 2: 소스에서 빌드

```bash
# 저장소 클론
git clone https://github.com/your-username/MonitorLauncher.git
cd MonitorLauncher

# 빌드
dotnet build MonitorLauncher.csproj -c Release

# 실행 파일 위치
# bin/Release/net8.0-windows/MonitorLauncher.exe
```

### 사용 방법

#### 기본 사용

1. 프로그램을 실행합니다 (관리자 권한 권장)
2. **모니터 선택**: 드롭다운에서 원하는 모니터 선택
3. **실행 파일 지정**: "찾아보기..." 버튼으로 프로그램 선택
4. **인자 입력** (선택): 프로그램 실행 시 필요한 인자 입력
5. **창 상태 선택**: 전체화면, 창모드, 복원 중 선택
6. **실행** 버튼 클릭

#### 프로필 저장 및 사용

**프로필 저장:**
1. 프로그램 경로, 모니터, 창 상태를 설정
2. "프로필 저장" 버튼 클릭
3. 프로필 이름 입력 (예: "게임 - 모니터 2")

**프로필 실행:**
- 프로필 목록에서 더블클릭하면 해당 설정으로 즉시 실행
- 또는 프로필 선택 후 "실행" 버튼 클릭

**프로필 삭제:**
- 프로필 선택 후 "프로필 삭제" 버튼 클릭

#### 워크스페이스 저장 및 복원

**워크스페이스 저장:**
1. 복원하고 싶은 앱 창을 원하는 모니터와 위치에 배치
2. 워크스페이스 탭에서 "현재 배치 가져오기" 클릭
3. 포함할 창만 체크
4. 워크스페이스 이름 입력 후 "저장" 클릭

**워크스페이스 실행:**
- 저장된 워크스페이스를 선택 후 "실행" 클릭
- 또는 트레이 메뉴의 "워크스페이스 실행"에서 바로 실행

**모든 창 주 모니터로 모으기:**
- 워크스페이스 탭에서 "모든 창 주 모니터로" 버튼 클릭
- 화면 밖에 있거나 비정상 위치에 있는 창을 주 모니터 중앙으로 이동

#### 시스템 트레이 사용

- 창을 닫을 때 "백그라운드 실행" 선택 시 시스템 트레이로 이동
- 트레이 아이콘 더블클릭으로 창 표시/숨김
- 트레이 아이콘 우클릭으로 메뉴 접근
- 트레이 메뉴의 "프로필 실행"에서 저장된 프로필을 바로 실행 가능
- 트레이 메뉴의 "워크스페이스 실행"에서 저장된 워크스페이스를 바로 실행 가능

### 프로필 저장 위치

프로필은 다음 위치에 저장됩니다:
```
%APPDATA%\MonitorLauncher\profiles.json
```

프로필 파일을 백업하거나 다른 컴퓨터로 복사하여 사용할 수 있습니다.

워크스페이스는 다음 위치에 저장됩니다:
```
%APPDATA%\MonitorLauncher\workspaces.json
```

## 🛠️ 기술 스택

- **언어**: C# 12.0
- **프레임워크**: .NET 8.0
- **UI**: Windows Forms
- **API**: Win32 API (P/Invoke)
- **빌드 시스템**: MSBuild / .NET SDK

## 📁 프로젝트 구조

```
MonitorLauncher/
├── MonitorLauncher.csproj  # 프로젝트 파일
├── app.manifest            # 관리자 권한 요구 설정
├── Program.cs              # 진입점
├── MainForm.cs             # 메인 UI 및 로직
├── AppLauncherService.cs   # 프로그램 실행 및 창 이동 조정
├── WorkspaceRestoreService.cs # 워크스페이스 복원 및 창 모으기
├── WindowCaptureService.cs # 현재 열려 있는 창 캡처
├── LaunchRequest.cs        # 실행 요청 모델
├── LaunchResult.cs         # 실행 결과 모델
├── WorkspaceProfile.cs     # 워크스페이스 저장/로드
├── AppWindowProfile.cs     # 워크스페이스 앱 창 모델
├── CapturedWindowInfo.cs   # 캡처된 창 정보
├── AppWindowState.cs       # 창 상태 enum
├── Win32Api.cs             # Win32 API 래퍼
├── WindowController.cs     # 창 위치 제어 로직
├── Profile.cs              # 프로필 저장/로드
├── Resources/              # 리소스 파일
│   ├── logo.png           # 앱 로고
│   └── favicon.ico        # 앱/폼/트레이 아이콘
├── tasks/                  # 작업 기록 및 교훈
├── .github/
│   └── workflows/
│       └── build.yml      # GitHub Actions 빌드 설정
└── README.md              # 이 파일
```

## ⚠️ 주의사항 및 제한사항

### 지원 제한

- **UWP 앱**: Windows 스토어 앱은 완벽하게 지원되지 않을 수 있습니다.
- **스플래시 스크린**: 일부 프로그램의 스플래시 스크린으로 인해 창 위치 제어가 지연될 수 있습니다.
- **관리자 권한**: 관리자 권한으로 실행되는 프로그램을 제어하려면 런처도 관리자 권한으로 실행해야 합니다.
- **런처형 앱/게임**: 자식 프로세스에서 실제 창을 띄우는 프로그램은 개선된 fallback을 사용하지만, 100% 보장되지는 않습니다.
- **워크스페이스 복원**: 앱이 실행 후 창 제목이나 프로세스 구조를 크게 바꾸는 경우 일부 창은 자동 감지되지 않을 수 있습니다.

### 권장 사항

- **관리자 권한 실행**: 대부분의 프로그램 창 제어를 위해 관리자 권한으로 실행하는 것을 권장합니다.
- **프로필 백업**: 중요한 프로필은 정기적으로 백업하세요 (`%APPDATA%\MonitorLauncher\profiles.json`)

## 🔧 개발 및 기여

### 개발 환경 설정

```bash
# .NET 8.0 SDK 설치 필요
dotnet --version  # 8.0.x 확인

# 프로젝트 복제
git clone https://github.com/your-username/MonitorLauncher.git
cd MonitorLauncher

# 의존성 복원
dotnet restore

# 빌드
dotnet build

# 실행
dotnet run
```

### 기여 방법

1. 이 저장소를 Fork
2. 기능 브랜치 생성 (`git checkout -b feature/AmazingFeature`)
3. 변경사항 커밋 (`git commit -m 'Add some AmazingFeature'`)
4. 브랜치에 Push (`git push origin feature/AmazingFeature`)
5. Pull Request 생성

### 버그 리포트

버그를 발견하셨다면 [Issues](https://github.com/your-username/MonitorLauncher/issues)에 리포트해주세요.

## 📝 변경 이력

### 최근 주요 업데이트

- **v2.0.0**: 여러 앱 창 배치를 저장/복원하는 워크스페이스 매니저 MVP 추가
- **v1.2.6**: 모니터 연결 변경 후 실행/프로필 저장 안전성 강화, 프로필 삭제 저장 흐름 보완
- **v1.2.5**: 셸 실행 창 추적 안정화, 창 보정 루프 수정, 새 창 이동 성공 판정 개선
- **v1.2.4**: 프로그램 실행 로직 서비스 분리, 실행 요청/결과 모델 추가, `MainForm` 책임 정리
- **v1.2.3**: 앱 로고 추가, GitHub Actions 자동 빌드/릴리스 설정
- **v1.2.2**: 성능 최적화 (CPU 사용량 40% 감소), 창 위치 제어 강화
- **v1.1.0**: 시스템 트레이 기능, 창 상태 옵션 개선

## 📄 라이선스

이 프로젝트는 자유롭게 사용, 수정, 배포할 수 있습니다.

## 🙏 감사의 말

이 프로젝트를 사용해주시고 기여해주시는 모든 분들께 감사드립니다.

---

<div align="center">

**⭐ 이 프로젝트가 도움이 되셨다면 Star를 눌러주세요! ⭐**

Made with ❤️ for Windows users

</div>
