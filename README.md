# Monitor Launcher v1.2.3

<div align="center">

![Monitor Launcher Logo](Resources/logo.png)

**Windows 다중 모니터 환경에서 프로그램을 특정 모니터에서 실행할 수 있게 해주는 경량 유틸리티**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?logo=windows)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-Free-green)](LICENSE)

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
- **창 상태 제어**: 전체화면, 창모드, 복원 옵션을 지원합니다.
- **시스템 트레이 지원**: 백그라운드 실행 및 트레이 아이콘을 통한 빠른 접근
- **자동 창 위치 모니터링**: 프로그램이 창 위치를 변경해도 자동으로 원하는 모니터로 재배치

## 주요 파일 구조

~~~
MonitorLauncher/
├── MonitorLauncher.csproj  # 프로젝트 파일
├── app.manifest            # 관리자 권한 요구 설정
├── Program.cs              # 진입점
├── MainForm.cs             # 메인 UI 및 로직
├── Win32Api.cs             # Win32 API 래퍼
├── WindowController.cs     # 창 위치 제어 로직
├── Profile.cs              # 프로필 저장/로드
└── README.md               # 문서
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

#### 시스템 트레이 사용

- 창을 닫을 때 "백그라운드 실행" 선택 시 시스템 트레이로 이동
- 트레이 아이콘 더블클릭으로 창 표시/숨김
- 트레이 아이콘 우클릭으로 메뉴 접근

### 프로필 저장 위치

프로필은 다음 위치에 저장됩니다:
```
%APPDATA%\MonitorLauncher\profiles.json
```

프로필 파일을 백업하거나 다른 컴퓨터로 복사하여 사용할 수 있습니다.

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
├── Win32Api.cs             # Win32 API 래퍼
├── WindowController.cs     # 창 위치 제어 로직
├── Profile.cs              # 프로필 저장/로드
├── AppWindowState.cs       # 창 상태 enum
├── Resources/              # 리소스 파일
│   └── logo.png           # 앱 로고
├── docs/                   # 문서
│   └── images/            # 스크린샷
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

자세한 변경 이력은 [CHANGELOG.md](CHANGELOG.md)를 참조하세요.

### 최근 주요 업데이트

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
