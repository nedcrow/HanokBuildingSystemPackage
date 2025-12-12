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
Assets/Scripts/
├── Core/                      # 핵심 시스템
│   ├── Building/             # 건물 시스템
│   │   ├── Building.cs
│   │   ├── BuildingResource.cs
│   │   ├── BuildingStatusData.cs
│   │   ├── DurabilityComponent.cs
│   │   ├── LaborComponent.cs
│   │   └── WallBuilding.cs
│   ├── Catalog/              # 카탈로그/풀링 시스템
│   │   ├── BuildingCatalog.cs
│   │   ├── BuildingMemberCatalog.cs
│   │   ├── HouseCatalog.cs
│   │   ├── ResourceTypeCatalog.cs  # NEW: 자원 타입 관리
│   │   └── CatalogBase.cs
│   ├── TypeDefinitions/      # NEW: ScriptableObject 타입 정의
│   │   ├── ResourceTypeData.cs    # 자원 타입 (계층 구조 지원)
│   │   ├── BuildingTypeData.cs    # 건물 타입
│   │   └── HouseTypeData.cs       # 주택 타입
│   ├── Common/               # 공통 컴포넌트
│   │   ├── RemodelingController.cs
│   │   └── IRemodelingRule.cs
│   ├── House/                # 집 시스템
│   │   ├── House.cs
│   │   └── MarkerComponent.cs
│   └── Plot/                 # 필지 시스템
│       ├── Plot.cs
│       └── PlotGenerator.cs
├── Attributes/               # NEW: 커스텀 어트리뷰트
│   └── ReadOnlyAttribute.cs
├── Editor/                   # 에디터 전용 스크립트
│   ├── BuildingEditor.cs
│   ├── WallBuildingEditor.cs
│   └── ReadOnlyDrawer.cs     # ReadOnly 어트리뷰트 렌더러
├── Utilities/                # 유틸리티
│   ├── Interface/           # UI 시스템
│   ├── Visualization/       # 시각화/규칙 시스템
│   │   ├── HBSRemodelingRule_Dam.cs
│   │   └── HBSRemodelingRule_Wall.cs
│   ├── HanokSystemController.cs
│   └── WallGenerator.cs      # 담장 자동 생성
└── Prefabs/                  # 프리팹 에셋
    ├── Buildings/
    ├── Houses/
    └── ResourceTypes/        # NEW: 자원 타입 ScriptableObject 에셋
```

## 주요 기능

### 건물 시스템
- 한옥 건물 배치 및 회전
- 건설 단계 관리 (Instant, TimeBased, LaborBased)
- 건물 내구도 시스템 (환경 저항, 열화)
- 담장 자동 생성 (WallGenerator)

### 타입 시스템 (ScriptableObject 기반)
- **확장 가능한 타입 정의**: 코드 수정 없이 Unity Inspector에서 새 타입 생성
- **ResourceTypeData**: 자원 타입 정의 (계층 구조 지원)
  - 부모-자식 관계: Wood → SoftWood, HardWood
  - 자동 호환성 체크: SoftWood가 Wood 요구사항 충족
- **BuildingTypeData**: 건물 타입 정의
- **HouseTypeData**: 주택 타입 정의

### 자원 관리
- **ResourceTypeCatalog**: 중앙화된 자원 관리
  - 자원 검색 및 호환성 확인
  - 대체 가능한 자원 자동 탐색
  - 계층 구조 기반 자원 분류

### 리모델링 시스템
- 건물 리모델링 규칙 (IRemodelingRule 인터페이스)
- 조건부 리모델링 검증
- 완료/취소 처리

### 기타
- 필지(Plot) 관리 시스템
- 건물/부재 카탈로그 및 오브젝트 풀링
- UI 인터페이스

## 사용 방법

### 타입 정의 생성하기

#### 1. 자원 타입 생성
```
1. Project 창에서 우클릭
2. Create > HanokBuildingSystem > TypeDefinitions > ResourceType
3. Inspector에서 설정:
   - Resource Type ID: 고유 식별자 (예: "wood", "softwood")
   - Display Type Name: 표시 이름 (예: "목재", "연목")
   - Parent Category: 상위 카테고리 (예: SoftWood의 parent는 Wood)
   - Icon, Description, Color 등
```

**자원 계층 구조 예시:**
```
Wood (최상위)
├── SoftWood (parentCategory: Wood)
└── HardWood (parentCategory: Wood)

Stone (최상위)
├── Granite (parentCategory: Stone)
└── Marble (parentCategory: Stone)
```

#### 2. 건물/주택 타입 생성
```
Create > HanokBuildingSystem > TypeDefinitions > [BuildingType / HouseType]
```

### 자원 호환성 활용하기

```csharp
// 건물이 Wood 10개를 요구
Cost requirement = new Cost(woodResourceData, 10);

// 플레이어가 SoftWood 보유
ResourceTypeData playerResource = softWoodResourceData;

// 호환성 체크 (SoftWood는 Wood 요구사항을 만족함)
if (requirement.CanBeSatisfiedBy(playerResource))
{
    // ✅ SoftWood로 건축 가능!
}
```

### ResourceTypeCatalog 설정

```
1. HanokBuildingSystem GameObject에 ResourceTypeCatalog 컴포넌트 추가
2. Inspector의 Resource Types 리스트에 모든 ResourceTypeData asset을 등록
3. 런타임에서 자원 검색 및 호환성 확인 가능
```

### 담장 자동 생성

```csharp
// WallBuilding 설정
wallBuilding.WallCenter = centerPrefab;
wallBuilding.WallEnd = endPrefab;
wallBuilding.WallSegmentLength = 1.5f;

// Plot 경계를 따라 담장 생성
wallGenerator.GenerateWallsForPlot(plot, wallBuilding);
```

## 버전

- 현재 버전: 0.1.0
- Unity 최소 버전: 6000.2.2f1

## 주요 변경사항

### v0.1.0 - 타입 시스템 리팩토링
- ✨ **ScriptableObject 기반 타입 시스템**: enum → ScriptableObject 마이그레이션
  - 사용자 확장 가능: 코드 수정 없이 새 타입 생성 가능
  - `TypeDefinitions/` 폴더 추가 (ResourceTypeData, BuildingTypeData, HouseTypeData)
- ✨ **자원 계층 구조**: 부모-자식 관계 지원
  - parentCategory 필드로 계층 정의
  - 자동 호환성 체크 (SoftWood → Wood)
- ✨ **ResourceTypeCatalog**: 중앙화된 자원 관리 시스템
  - 자원 검색 및 대체 가능한 자원 탐색
  - 계층 구조 기반 자원 분류
- ♻️ **BuildingResource 리팩토링**: Cost 구조체 개선
  - ResourceTypeData 사용 (enum 대신)
  - 호환성 체크 메서드 추가 (CanBeSatisfiedBy, IsCompatibleWith)
- 🔥 **Deprecated 제거**: enum 타입 시스템 완전 제거
  - `Utilities/ConstructionType/` 폴더 삭제
- ✨ **ReadOnlyAttribute**: Inspector 읽기 전용 필드 지원
  - `Attributes/` 폴더 추가
- ✨ **WallGenerator**: 담장 자동 생성 시스템
  - Plot 경계 따라 담장 자동 배치
  - 타일 정렬 및 회전 처리

## 라이선스

MIT License
