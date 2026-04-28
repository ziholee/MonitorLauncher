# Git Ignore Cleanup

## Checklist
- [x] 현재 `.gitignore` 상태와 추적 중인 불필요 파일을 점검한다
- [x] 프로젝트에 맞는 ignore 규칙을 추가한다
- [x] 이미 추적 중인 불필요 파일을 Git 인덱스에서 정리한다
- [x] 변경 결과를 검증하고 기록한다

## Review
- 현재 저장소에는 `.gitignore`가 없었고, Git이 추적 중인 불필요 파일은 루트의 `.DS_Store` 하나였다.
- `.gitignore`에 macOS, .NET 빌드 산출물, Visual Studio/에디터 관련 ignore 규칙을 추가했다.
- `git rm --cached .DS_Store`로 `.DS_Store`를 Git 인덱스에서 제거했다. 로컬 파일은 남겨 두었다.
- 검증: `git ls-files` 기준으로 `.DS_Store`, `bin/`, `obj/`, `.vs/`, `.idea/`, `.vscode/` 같은 불필요 항목은 더 이상 추적되지 않는다.
