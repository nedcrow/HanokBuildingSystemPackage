# HanokBuildingSystem
한옥 타입에 맞춰 내부 건물들을 설정할 수 있습니다. 건축부지를 그리면 기본 설정에 맞춰 건물이 토대를 잡습니다. 내부 건물은 각자 **0~100%** 까지 완성도를 가집니다. 내부 건물들은 **건설 이후에도 추가, 위치 및 각도 수정이 가능**합니다.

## Test UseCase
### Dev (개발자 설정)
1. 씬에 **HanokBuildingSystem** 생성
2. 사용할 **BuildingMember, Building, House Prefab** 준비 (적절한 Component 부착)
3. 각 Building **완성조건, 내구도** 등 프로퍼티 등록, 각 House **필수 빌딩** 등 프로퍼티 등록
4. 사용할 BuildingMember, Building, House Prefab들을 각 **Catalog에 등록**. Catalog는 필요에 따라 늘릴 수 있다.
5. 중요 함수 **인터페이스 연결**

### User (사용자 플레이)
1. 원하는 **House 타입 선택** (빌딩 시스템 활성화 `SwitchCurrentActionMap("맵이름")`)
2. **Plot 위치 설정**
3. **Plot 영역 설정** (4 꼭지점)
4. 개발에서 설정된 **완성 조건 달성하기** (기다리기)
5. **Building 위치 및 방향 수정**, 또는 추가.


## System State (InputSystem에서 해당 action map을 가짐)
- **Off**: 사용 안 할 때. 좌클릭 등 인풋이 들어와도 무시.
- **NewBuilding**: 새 건축을 건설할 때 기준. Plot, House, Building에 영향을 미침.
- **Remodeling**: 이미 지어진 건축의 리모델링 상황의 인풋 기준. House, Building에 영향을 미침.
- **Remove**: 드래그로 사각형 영역을 그리고 겹치는 건물 철거(또는 철거 명령)


## Placement Rule
- **최대 경사각**: Plot의 가장 높은 점과 낮은 점이 평지와 만드는 경사각을 통해 제한 가능. max값이 0이면 허용.
- **최소 XY 크기**: 선택된 House의 최소 X,Y 크기보다 만들어질 Plot의 X,Y 크기가 넓어야함.
- **문 위치**: 기본적으로 첫번째 라인 중앙에 문이 생김. 리모델링 시 위치 수정, 추가 가능.
- **문 수량**: 대문 수량을 통제하며 **4개 이상은 불가**하고 **한 라인에 하나만** 가능.


## Remodeling System
리모델링은 이미 건설된 House의 Building 구성을 편집하고, 이를 청사진(Blueprint)으로 저장하여 재사용하는 시스템입니다.

### 리모델링 가능 작업
    1. **옵셔널 Building 추가**: 
        - House에 필수가 아닌 추가 Building 배치 (예: 창고, 정자, 연못)
        - BuildingCatalog에서 원하는 BuildingType 선택하여 배치
        
    2. **Building 삭제**: 
        - 필수 Building은 삭제 불가 (RequiredBuildingTypes에 포함된 것)
        - 옵셔널 Building만 선택하여 제거
        - 제거된 Building은 BuildingCatalog로 반환
        
    3. **Building 위치 및 각도 수정**: 
        - 기존 Building의 Transform(Position, Rotation) 조정
        - House 경계 내에서만 이동 가능
        
    4. **Building 증축**: 
        - Building의 Size 프로퍼티 수정
        - constructionStage를 조정하여 확장 건설 표현
        - 증축 비용은 Building의 Cost 리스트 참조

### Blueprint (청사진) 저장
리모델링한 House 구성을 템플릿으로 저장하여 새 House 건설 시 재사용할 수 있습니다.

#### HouseBlueprint 구조
    - **blueprintName**: 청사진 이름 (예: "대형한옥_창고형", "소형한옥_기본형")
    - **baseHouseType**: 기반이 되는 HouseType
    - **buildingConfigs**: Building 구성 정보 리스트
        -- buildingType: Building 타입
        -- localPosition: House 기준 상대 위치
        -- localRotation: House 기준 상대 회전
        -- size: Building 크기 (증축 정보)
        -- isRequired: 필수 여부
        -- constructionStage: 건설 단계
    - **totalCost**: 전체 건설 비용 (기본 + 증축)
    - **createdDate**: 생성 날짜
    - **thumbnailSprite**: 미리보기 이미지 (옵션)

#### Blueprint 저장 흐름
    1. 리모델링 완료 후 "청사진으로 저장" 버튼 클릭
    2. HanokBuildingSystem.SaveAsBlueprint(House house, string blueprintName) 호출
    3. 현재 House의 모든 Building 정보를 HouseBlueprint로 직렬화
    4. BlueprintCatalog에 저장 (ScriptableObject 또는 JSON 파일)
    5. 저장된 Blueprint는 UI에서 목록으로 확인 가능

#### Blueprint 불러오기
    - **NewBuilding 모드에서 사용**: Plot 완성 후 House 배치 시 Blueprint 선택
    - **선택한 Blueprint의 Building 구성을 자동 배치**
    - HanokBuildingSystem.ApplyBlueprint(House house, HouseBlueprint blueprint) 호출
    - Blueprint의 buildingConfigs를 순회하며:
        1. BuildingCatalog에서 해당 타입의 Building 가져오기
        2. localPosition, localRotation을 World 좌표로 변환하여 배치
        3. size, constructionStage 등 프로퍼티 적용
        4. House.AddBuilding()으로 Building 등록

### 리모델링 완료
    1. **저장하고 완료**: 
        - 변경사항을 House에 적용
        - UsageState를 원래 상태로 복원 (Occupied/Vacant)
        - SystemState를 Off로 전환
        
    2. **청사진으로 저장하고 완료**: 
        - 위 저장 작업 수행
        - HouseBlueprint 생성 및 BlueprintCatalog에 저장
        
    3. **취소**: 
        - 변경사항을 모두 롤백
        - 추가된 Building은 BuildingCatalog로 반환
        - 삭제된 Building은 다시 복구
        - UsageState를 원래 상태로 복원



## Use Prefab Types
### HanokBuildingSystem
- **필수 Component**: `HanokBuilder`, `PlotController`
- **건설모드** (## System State):
    - **Off**: CurrentPlot 비활성화
    - **NewBuilding**: CurrentPlot 생성
    - **Remodeling**: CurrentHouses[0] 수정 준비
- **인터페이스**:
    - **마우스 좌클릭**: Off 상태에서 CurrentHouses Raycasting, NewBuilding 상태에서 vertex 생성
    - **마우스 좌클릭 + 드래그**: 범위 내 House 탐색. 찾으면 단일 또는 그룹 정보 패널 활성화
    - **마우스 더블클릭**: 화면 내 House 다중 탐색. 찾으면, 단일 또는 그룹 정보 패널 활성화
    - **마우스 우클릭**: Off 상태에서 CurrentHouses 초기화, NewBuilding 상태에서 마지막 Vertex 취소 및 Vertex없는 Plot 삭제
    - **마우스 이동**: CurrentPlot 가시화 명령 및 CurrentHouses 위치 업데이트
- **건설 순서**: 점, 선, 면
    1. **점**: 임의 위치에 CurrentPlot의 점 생성. 처음에 두 점 생성 하고 한 점은 커서 따라 이동.
    2. **선**: CurrentPlot의 마지막 점과 바로 앞 점 사이, 만약 점 숫자가 최대면 마지막과 첫 점 사이에도 라인 생성. 라인은 기본적으로 직선이지만 사이에 `RoadComponent` 발견 시 곡선도 가능.
    3. **면**: plot의 점이 셋 이상일 때 면을 생성.
    4. CurrentHouses **미리배치**.
    5. CurrentHouses plot 안에 **들어갈 수 있는지 검사**.
    6. Plot xy 크기가 CurrentHouses의 최소 xy 크기보다 **두 배 이상**이고 **분할 옵션이 켜져있으면** PlotController에 분할 요청
    7. CurrentHouse 내부에 **필요 building 위치 파악** 후 미리보기용 배치.
    8. **기타 건설 조건 필터**
    9. CurrentHouses **모두 건설 가능 판단 시** 모든 House **건설중 모드로 변경**.
    10. **건설 취소 시**, 각 카탈로그에 House와 Building, BuildingMember 반환.
    11. 건설모드 **Off**.


### BuildingMember
- **BuildingMember 클래스는 없다**. 단순히 구분을 위한 명칭.

### Building
- **Building** 또는 Building을 상속 받은 컴포넌트를 가진 GameObject.
- 자원이 모두 모였을 때, **'즉시건설, 임의 시간 딜레이, 임의 노동력 할당(HP)건설진행'** 중 방법을 선택 가능하다.
- 자식으로 여러 **BuildingMember**를 갖는다.

### House
- **House** 또는 House를 상속 받은 컴포넌트를 가진 GameObject.
- 인스펙터로 많은 **기본 설정**이 필요하다.
- 미리 사용할 빌딩 프리팹을 자식으로 갖지는 않지만 **위치 확인용 마커는 가진다**.
- **MarkerComponent** 가진다.


## Use Components: MonoBehaviour
### Building
- Transform 외에 **별도의 크기**를 가진다. 크기는 외부 수정 가능하다.
- **건설단계, 환경상태**는 수량이 수정 가능한 `int` 형식이다.
- 건설단계, 환경상태는 Building을 상속한 클래스에서 **가독성을 위한 가공을 권고**한다.
- **건설 단계는 앞 단계가 이뤄져야** 다음 단계로 변경 가능하다.
- 건설 단계가 진행되면 **각 단계별 자식 GameObject를 활성화** 시킨다.
- **비용(Cost struct) 리스트**를 가진다.
- **CRUD**

#### 건설 진행 모드 (ConstructionMode)
Building은 **3가지 건설 방식** 중 하나를 선택할 수 있습니다:

1. **Instant (즉시건설)**
   - 자원이 모두 모이면 **즉시 완성**
   - 단계 진행이 즉각적

2. **TimeBased (시간 기반)**
   - 각 단계마다 **일정 시간**이 지나면 자동으로 진행
   - `constructionDuration` 필드로 단계당 소요 시간 설정 (초 단위)
   - `SetConstructionMode(ConstructionMode.TimeBased)` 호출 시 자동으로 코루틴 시작

3. **LaborBased (노동력 기반)**
   - 일꾼이 **망치질할 때마다** 건설 진행도가 증가
   - **LaborComponent 필요** (자동으로 추가됨)
   - `LaborComponent.AddLabor(float amount)` 메서드 호출로 진행
   - **Building 인스펙터에서 설정 가능**:
     - `requiredLaborPerStage`: 각 단계당 필요 노동량 (기본 100)
     - `laborPerHit`: 망치질 1회당 노동량 (기본 1)
     - ConstructionMode를 LaborBased로 선택하면 **잠금 해제**됨
   - `currentLabor`: 현재 누적 노동량 (HP처럼 동작)
   - 필요 노동량을 채우면 다음 단계로 자동 진행
   - 사용 예: 일꾼 시뮬레이션, 실시간 건설 게임

#### 건설 진행도 프로퍼티
- **ConstructionProgress**: 전체 진행도 (0~1), 현재 단계 / 전체 단계
- **StageProgress**: 현재 단계의 진행도 (0~1)
  - Instant: 항상 1
  - TimeBased: 코루틴 진행 중이면 0.5, 대기 중이면 0
  - LaborBased: currentLabor / requiredLaborPerStage (LaborComponent 참조)


### LaborComponent
- **노동력 기반 건설**을 담당하는 컴포넌트
- `Building`의 ConstructionMode가 `LaborBased`일 때 **자동으로 추가됨**
- **RequireComponent(typeof(Building))** - Building과 함께 사용
- 주요 기능:
  - `AddLabor(float amount)`: 망치질할 때 호출, 노동량 추가
  - `ResetLabor()`: 현재 단계의 노동량 초기화
  - `LaborProgress`: 현재 단계의 진행도 (0~1)
- 설정 가능:
  - `requiredLaborPerStage`: 각 단계당 필요 노동량 (기본 100)
  - `laborPerHit`: 망치질 1회당 노동량 (기본 1)
- DurabilityComponent의 `TakeDamage`와 **유사한 구조** (방향만 반대)
  - TakeDamage: 내구도 **감소** (⬇️)
  - AddLabor: 건설 진행도 **증가** (⬆️)

### DurabilityComponent
- **내구도(HP)** 가짐.
- 데미지를 받으면 **저항치를 반영**해서 내구도를 감한다.
- 수리를 받으면 내구도가 회복된다. **비용은 해당 건물의 비용의 손상된 내구도비율 만큼**이다.
- **환경 상태**(정상, 화재, 비, 적설, 낡음)는 **머티리얼의 프로퍼티**에 영향을 준다.
- **파손 상태**는 건물의 외형을 변하게 한다. (Prefab 또는 StaticMesh).

### House
- **샘플 크기 프로퍼티**를 참조하여 EditorScene에 **노란 바운더리** 표시.
- 샘플 크기 프로퍼티를 수정하면 **EditorScene에도 바로 수정치가 반영**.
- 마커 크기 프로퍼티를 수정하면 **EditorScene에도 바로 수정치가 반영**.
- 구성에 **반드시 필요한 BuildingType 목록**을 가짐.
- **현재 구성된 Building 목록**을 가짐.
- Edit Mode 일 때, BuildingType 목록 수량이 변하면 그에 맞춰 하위에 **BuildingMarker GameObject 생성/삭제**.
- **프로퍼티**: 소유자(NPC), 거주인원, 거주가능인원, 창고, 창고용량, 전체면적(제곱미터), 만족조건들, 만족한 조건들, 만족도, 내구도(HP), 상태(정상, 화재, 침수, 적설, 폐가) 표시.
- **집 사용 상태**: 건설중(`UnderConstruction`), 빈집(`Vacant`), 거주중(`Occupied`) 표시
- **CRUD**

### MarkerComponent
- **메인 마커와 하위 마커들을 관리**하는 컴포넌트.
- **메인 마커**는 xy 크기 조정이 가능하며 EditorScene에 **노란 바운더리** 표시.
- **하위 마커들**은 xy 크기와 위치 조정이 가능하며 EditorScene에 **주황 바운더리** 표시.

### Catalog
#### Catalog 개요
해당 Catalog에 등록한 **프리팹 리스트의 인스턴스 풀링을 담당**합니다.
- **HouseCatalog**: Inspector에 등록되는 프리팹이 `House` 컴포넌트를 가졌는지 필터
- **BuildingCatalog**: Inspector에 등록되는 프리팹이 `Building` 컴포넌트를 가졌는지 필터
- **BuildingMemberCatalog**: Inspector에 등록되는 프리팹이 Building/House를 **가지지 않았는지** 필터

#### Catalog 클래스 기본기능
각 Catalog는 **여러 프리팹을 관리**하고, 프리팹마다 `PoolingComponent`를 생성합니다.
- **Initial Pool Size Per Prefab**: 각 프리팹당 초기 풀 크기 (기본 5~10)
- **Auto Expand**: 풀이 비었을 때 자동 확장 여부
- 사용할 프리팹들을 **리스트에 추가**
- 유효하지 않은 프리팹은 `OnValidate`에서 **자동 제거**
- 모든 Catalog는 **CatalogBase<T>를 상속**하여 공통 풀링 로직 공유

#### HouseCatalog
- **House 컴포넌트가 있는 프리팹만 등록 가능**
- Inspector에서 House 컴포넌트 없는 프리팹 **자동 필터링**
- `GetHouse()`, `GetHouse(prefab)`, `GetHouse(prefabIndex)` 등으로 **House 가져오기**
- `ReturnHouse(house)`로 풀에 **반환**

#### BuildingCatalog
- **Building 컴포넌트가 있는 프리팹만 등록 가능**
- `GetBuildingByType(BuildingType)` 으로 **타입별 Building 가져오기**
- `GetBuilding(prefab)`, `GetBuilding(prefabIndex)` 지원
- `ReturnBuilding(building)`로 풀에 **반환**

#### BuildingMemberCatalog
- **일반 GameObject 프리팹 관리** (Building/House 컴포넌트 없음)
- `GetMemberByName(string)` 으로 **이름별 GameObject 가져오기**
- `GetMember(prefab)`, `GetMember(prefabIndex)` 지원
- `ReturnMember(member)`로 풀에 **반환**

#### 권장 사용 패턴
**Catalog를 메인으로 사용**하고, 특별한 풀링 요구사항이 있을 때만 Common 폴더의 Pool 사용


### PoolingComponent
- **기본 GameObject 풀링 컴포넌트**
- **단일 프리팹**의 인스턴스 풀링 담당
- `Get()` / `Return()` 기능 제공
- Catalog 내부에서 자동 생성 및 관리

### PlotController
Plot을 매개변수 삼는 **유틸 함수 모음**.
- `ShowPlot(Plot plot)`: **Mesh 및 Line**으로 Plot 영역을 명확히 표시
- `DividePlot_Horizontal(Plot, count)`: Plot을 특정 수량만큼 **가로 분할**
- `DividePlot_Vertical(Plot, count)`: Plot을 특정 수량만큼 **세로 분할**
- **LineStyle**: Solid(실선) / Dashed(점선) 스타일 지원

### HouseManager
- 인터페이스를 통해 순차적으로 **Plot, House, Building 생성 및 유지보수**를 관리한다.
- 활성화된 **House GameObject를 자식으로 보관**한다.
- **인터페이스**: 유니티 URP 버전 이후의 인풋 시스템 사용
    - **마우스 좌클릭**: Plot준비, Plot 꼭지점 생성, Plot 및 하우스 업데이트에 사용
    - **마우스 우클릭**: 꼭지점 취소, Plot 및 하우스 업데이트에 사용
    - **마우스 커서 움직임**: Plot 및 하우스 업데이트에 사용
    - **Shift + 마우스 커서 움직임**: 미리보는 plot 꼭지점 각도(45 degree) **스냅핑**에 사용


## Use Structures
### Plot
- **한옥 건설을 요청할 부지**.
- **최소 네 개의 꼭지점**과 선, 하나의 면을 갖는다.
- 각 꼭지점의 **내각**은 최소, 최대 각도(degree)를 갖는다.
- 각 선은 **공통의 두께**를 갖는다.
- **CRUD 함수**를 갖는다.
- **건설 가능 boolean 상태**(`IsBuildable`)를 가진다.
- **LineList** 구조: `List<List<Vector3>>`로 다중 라인 관리


## Custom Material
**정상, 화재, 비, 적설, 낡음** 상태의 프로퍼티를 가짐.
- **정상**: 기본 머티리얼
- **화재**: 기본 머티리얼에 **emissive와 범위를 정하는 추가 텍스쳐**로 불타는 효과
- **적설**: 기본 머티리얼에 범위를 정하는 추가 텍스쳐로 **눈 쌓인 효과**
- **비**: 기본 머티리얼에 범위를 정하는 추가 텍스쳐(적설용 텍스쳐와 같음)로 **물방울들이 무작위로 퍼지는 효과**
- **낡음**: 기본 머티리얼에 범위를 정하는 추가 텍스쳐로 **썩거나 녹쓸고 이끼낀 효과**