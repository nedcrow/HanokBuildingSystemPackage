# Repository Structure

Unity Package로 배포되는 한옥 건물 시스템의 저장소 구조입니다.

## 개요

이 저장소는 크게 두 부분으로 구성됩니다:
- **HanokBuildingSystem/**: Unity 개발 프로젝트 (개발 및 테스트용)
- **package/**: Unity Package 배포용 폴더 (실제 패키지 콘텐츠)

## 루트 구조

```
HanokBuildingSystemPackage/
├── .claude/                          # Claude Code 설정
├── .serena/                          # Serena 설정
├── .git/                             # Git 저장소
│   └── hooks/
│       └── pre-commit               # 자동 동기화 훅 (Assets/Scripts → package)
├── .gitignore                        # Git 무시 파일 설정
├── LICENSE                           # 라이선스 파일
├── README.md                         # 프로젝트 소개 문서
├── PROJECT_COMPLETENESS_REPORT.md    # 프로젝트 완성도 리포트
├── REPOSITORY_STRUCTURE.md           # 이 문서
├── construction-tree.bat             # 프로젝트 구조 출력 스크립트
│
├── HanokBuildingSystem/              # Unity 개발 프로젝트
│   ├── .vscode/                      # VSCode 설정
│   ├── Assets/                       # Unity 에셋 폴더
│   ├── Packages/                     # Unity 패키지 매니페스트
│   ├── ProjectSettings/              # Unity 프로젝트 설정
│   ├── Library/                      # Unity 라이브러리 (gitignore)
│   ├── Temp/                         # Unity 임시 파일 (gitignore)
│   ├── Logs/                         # Unity 로그 (gitignore)
│   ├── UserSettings/                 # Unity 사용자 설정 (gitignore)
│   └── *.sln, *.csproj               # Unity 솔루션/프로젝트 파일 (gitignore)
│
└── package/                          # Unity Package (배포용)
    ├── Runtime/                      # 런타임 스크립트
    ├── Editor/                       # 에디터 스크립트
    ├── Samples/                      # 샘플 콘텐츠 (예정)
    └── package.json                  # 패키지 매니페스트
```

## HanokBuildingSystem/ (개발 프로젝트)

개발 및 테스트를 위한 Unity 프로젝트입니다.

### Assets/ 구조

```
Assets/
├── Scripts/                          # 스크립트 (소스 원본)
│   ├── Core/                         # 핵심 시스템
│   │   ├── Building/                 # Building 관련
│   │   ├── House/                    # House 관련
│   │   ├── Plot/                     # Plot 관련
│   │   ├── Catalog/                  # 오브젝트 풀링 카탈로그
│   │   ├── Common/                   # 공통 컴포넌트
│   │   ├── TypeDefinitions/          # 타입 정의 (ScriptableObject)
│   │   ├── HanokBuildingSystem.cs    # 메인 시스템 (싱글톤)
│   │   └── HanokBuildingSystemEvents.cs  # 이벤트 시스템
│   │
│   ├── Utilities/                    # 유틸리티
│   │   ├── Interface/                # UI 관련
│   │   ├── Sample/                   # 샘플 구현
│   │   └── HanokBuildingSystemInput_Actions.cs
│   │
│   ├── Attributes/                   # 커스텀 Attribute
│   │   └── ReadOnlyAttribute.cs
│   │
│   └── Editor/                       # 에디터 확장
│       ├── BuildingEditor.cs
│       ├── BuildingMemberEditor.cs
│       ├── ConstructionResourceComponentEditor.cs
│       └── ReadOnlyDrawer.cs
│
├── Prefabs/                          # 프리팹
│   ├── BuildingMembers/              # 건물 부재
│   ├── Buildings/                    # 건물 타입 및 상태 (ScriptableObject)
│   ├── Houses/                       # 집 타입 (ScriptableObject)
│   ├── ResourceTypes/                # 자원 타입 (ScriptableObject)
│   ├── UI/                           # UI 프리팹
│   └── HanokBuildingSystemSample.prefab
│
├── Scenes/                           # 샘플 씬
│   ├── SampleScene.unity
│   └── SampleScene2.unity
│
├── Meshes/                           # 3D 메시
│   ├── Sample_Map.fbx
│   └── cube.fbx
│
├── Materials/                        # 머티리얼
│   ├── DottedLine.png
│   └── M_DottedLine.mat
│
├── Fonts/                            # 폰트
│   └── NanumFontSetup_TTF_GOTHIC/
│
├── Settings/                         # 렌더링 설정
│   ├── PC_RPAsset.asset
│   ├── Mobile_RPAsset.asset
│   └── ...
│
├── TextMesh Pro/                     # TextMesh Pro 에셋
│
├── HBS_InputActions.inputactions     # Input System 액션
├── README.md                         # 패키지 설명
└── Readme.asset                      # Unity에서 보이는 README
```

## package/ (배포 패키지)

Unity Package Manager를 통해 배포되는 실제 패키지 콘텐츠입니다.

### 구조

```
package/
├── Runtime/                          # 런타임 스크립트
│   ├── Core/                         # 핵심 시스템
│   │   ├── Building/                 # Building 관련 (8개 파일)
│   │   ├── House/                    # House 관련 (2개 파일)
│   │   ├── Plot/                     # Plot 관련 (2개 파일)
│   │   ├── Catalog/                  # 카탈로그 (5개 파일)
│   │   ├── Common/                   # 공통 (3개 파일)
│   │   ├── TypeDefinitions/          # 타입 정의 (3개 파일)
│   │   ├── HanokBuildingSystem.cs
│   │   └── HanokBuildingSystemEvents.cs
│   │
│   ├── Utillities/                   # 유틸리티 (오타: Utilities)
│   │   ├── Interface/
│   │   ├── Sample/
│   │   └── HanokBuildingSystemInput_Actions.cs
│   │
│   ├── Attributes/                   # 커스텀 Attribute
│   │   └── ReadOnlyAttribute.cs
│   │
│   └── Nedcrow.HanokBuildingSystem.Runtime.asmdef
│
├── Editor/                           # 에디터 스크립트
│   ├── BuildingEditor.cs
│   ├── BuildingMemberEditor.cs
│   ├── ConstructionResourceComponentEditor.cs
│   ├── ReadOnlyDrawer.cs
│   └── Nedcrow.HanokBuildingSystem.Editor.asmdef
│
├── Samples/                          # 샘플 콘텐츠 (예정)
│   └── (비어있음)
│
└── package.json                      # 패키지 매니페스트
```

## 자동 동기화 시스템

### Git Pre-commit Hook

커밋 시 자동으로 다음 작업을 수행합니다:

1. **스크립트 동기화**
   - `HanokBuildingSystem/Assets/Scripts/` → `package/`로 복사
   - `.meta` 파일은 자동 제거
   - 동기화 대상:
     - Core → package/Runtime/Core/
     - Utilities → package/Runtime/Utillities/
     - Attributes → package/Runtime/Attributes/
     - Editor → package/Editor/

2. **버전 자동 증가**
   - `package/package.json`의 패치 버전 자동 증가
   - 예: 0.1.19 → 0.1.20

3. **자동 스테이징**
   - 동기화된 파일들을 자동으로 git에 스테이징

### 훅 파일 위치

`.git/hooks/pre-commit`

## 워크플로우

### 개발 시

1. `HanokBuildingSystem/Assets/Scripts/`에서 코드 수정
2. Unity Editor에서 테스트
3. `git add` 및 `git commit` 실행
4. Pre-commit 훅이 자동으로 `package/`로 동기화
5. 버전이 자동으로 증가되고 함께 커밋됨

### 배포 시

`package/` 폴더가 Unity Package로 배포됩니다:
```
https://github.com/nedcrow/HanokBuildingSystemPackage.git
```

## 주요 파일

- **package/package.json**: 패키지 메타데이터 (버전, 의존성 등)
- **.gitignore**: Unity 빌드 아티팩트, 임시 파일 제외
- **README.md**: 사용자용 프로젝트 소개
- **PROJECT_COMPLETENESS_REPORT.md**: 개발 완성도 리포트
- **.git/hooks/pre-commit**: 자동 동기화 스크립트

## 주의사항

1. **직접 편집 금지**: `package/Runtime/`, `package/Editor/` 폴더는 직접 편집하지 마세요. 커밋 시 덮어씌워집니다.
2. **소스 원본**: 항상 `HanokBuildingSystem/Assets/Scripts/`에서 작업하세요.
3. **버전 관리**: 버전은 자동으로 관리되지만, 메이저/마이너 버전 변경 시에는 수동으로 `package/package.json`을 수정하세요.
4. **경로 일관성**: 구조 변경 시 `.git/hooks/pre-commit` 스크립트도 함께 업데이트하세요.
