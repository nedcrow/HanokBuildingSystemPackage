# Hanok Building System Package

Unity 패키지로 만든 한옥 건물 시스템입니다.

## 설치 방법

### Git URL로 설치

1. Unity Editor에서 Window > Package Manager를 엽니다
2. '+' 버튼 클릭 > "Add package from git URL..." 선택
3. 다음 URL을 입력합니다:
   ```
   https://github.com/harim/HanokBuildingSystemPackage.git
   ```

## 패키지 구조

```
com.harim.hanokbuildingsystem/
├── package.json          # 패키지 메타데이터
├── README.md
├── Editor/              # 에디터 전용 스크립트
│   ├── BuildingEditor.cs
│   └── Harim.HanokBuildingSystem.Editor.asmdef
├── Runtime/             # 런타임 스크립트
│   ├── Core/           # 핵심 시스템
│   │   ├── Building/   # 건물 시스템
│   │   ├── Catalog/    # 카탈로그 시스템
│   │   ├── Common/     # 공통 컴포넌트
│   │   ├── House/      # 집 시스템
│   │   └── Plot/       # 필지 시스템
│   ├── Utillities/     # 유틸리티
│   │   ├── ConstructionType/
│   │   ├── Interface/  # UI 시스템
│   │   └── Visualization/
│   ├── Prefabs/        # 프리팹
│   │   ├── Buildings/  # 건물 프리팹
│   │   ├── Houses/     # 집 프리팹
│   │   └── UI/         # UI 프리팹
│   ├── Materials/      # 머티리얼
│   ├── HanokBuildingSystemInput_Actions.inputactions
│   └── Harim.HanokBuildingSystem.Runtime.asmdef
└── Samples~/           # 샘플 파일
```

## 기능

- 한옥 건물 배치 시스템
- 건물 리모델링 시스템
- 필지(Plot) 관리 시스템
- 건물 카탈로그 시스템
- UI 인터페이스

## 버전

- 현재 버전: 0.1.0
- Unity 최소 버전: 2021.1

## 라이선스

MIT License
