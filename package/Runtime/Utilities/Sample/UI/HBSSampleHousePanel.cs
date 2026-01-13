using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HanokBuildingSystem;
using System.Collections.Generic;
using System;

/// <summary>
/// [Sample] House 정보 표시 및 리모델링 워크플로우 UI 패널
///
/// 샘플 용도:
/// - House 정보(거주민, 내구도, 저장소 등) UI 표시 예제
/// - 리모델링 모드 진입/완료/취소 워크플로우 데모
/// - 리모델링 중 Building 추가 기능 예제
/// - HanokBuildingSystem 이벤트 기반 UI 업데이트 패턴 예제
///
/// 사용 방법:
/// 1. UI Canvas에 Panel GameObject 생성
/// 2. 이 컴포넌트를 Panel에 추가
/// 3. Inspector에서 TextMeshProUGUI 및 Button UI 요소 할당
/// 4. 버튼의 OnClick 이벤트에 해당 메서드 연결:
///    - OnClickRemodelingButton(): 리모델링 모드 진입
///    - OnClickCancelButton(): 리모델링 취소 (백업 복원)
///    - OnClickConfirmButton(): 리모델링 완료 (UnderConstruction 전환)
///    - OnClickAddBuilding(): 리모델링 중 Building 추가
///    - OnClickFillAllResources(): [디버그] 모든 자원 완납
///
/// 이벤트 연동:
/// - OnHouseSelected: House 선택 시 정보 갱신
/// - OnRemodelingStarted: 리모델링 시작 시 UI 모드 전환
/// - OnRemodelingCompleted: 리모델링 완료 시 UI 모드 복귀
/// - OnRemodelingCancelled: 리모델링 취소 시 UI 모드 복귀
/// </summary>
public class HBSSampleHousePanel : MonoBehaviour
{
    [Header("UI Elements - Basic Info")]
    [SerializeField] private TextMeshProUGUI houseName;
    [SerializeField] private TextMeshProUGUI houseType;
    [SerializeField] private GameObject informationPanel;
    [SerializeField] private GameObject remodelingPanel;

    [Header("Building List ScrollView")]
    [SerializeField] private GameObject buildingScrollView;
    [SerializeField] private RectTransform buildingListContent;
    [SerializeField] private float buildingSlotHeight = 50f;
    private List<GameObject> buildingSlots = new List<GameObject>();

    [Header("UI Elements - Status")]
    [SerializeField] private TextMeshProUGUI residents;
    [SerializeField] private TextMeshProUGUI durability;
    [SerializeField] private TextMeshProUGUI usageState;

    [Header("UI Elements - Storage (Optional)")]
    [SerializeField] private TextMeshProUGUI storage;
    [SerializeField] private Slider durabilitySlider;

    [Header("Buttons (Optional)")]
    [SerializeField] private Button remodelingButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;

    [Header("Debug Buttons")]
    [SerializeField] private Button fillAllResourcesButton;

    [Header("Remodeling - Add Building")]
    [SerializeField] private GameObject buildingPrefabToAdd;

    [Header("Remodeling - Eraser Mode")]
    [SerializeField] private Button eraserButton;
    [SerializeField] private UnityEngine.UI.Image eraserButtonImage;
    [SerializeField] private Color eraserActiveColor = Color.red;
    [SerializeField] private Color eraserInactiveColor = Color.white;

    private House currentHouse;
    private HanokBuildingSystem.HanokBuildingSystem buildingSystem;
    private bool isEraserMode = false;

    void Start()
    {
        if (buildingSystem == null)
        {
            buildingSystem = HanokBuildingSystem.HanokBuildingSystem.Instance;

            buildingSystem.Events.OnRemodelingStarted += HandleRemodelingStarted;
            buildingSystem.Events.OnRemodelingCompleted += HandleRemodelingCompleted;
            buildingSystem.Events.OnRemodelingCancelled += HandleRemodelingCancelled;
            buildingSystem.Events.OnBuildingModified += HandleBuildingModified;
        }

        // Building 슬롯 초기화 (Content 하위의 자식들을 가져오기)
        InitializeBuildingSlots();
    }

    /// <summary>
    /// Content 하위의 미리 준비된 슬롯들을 리스트에 추가
    /// </summary>
    private void InitializeBuildingSlots()
    {
        // buildingListContent가 할당되지 않았으면 자동으로 찾기 시도
        if (buildingListContent == null && buildingScrollView != null)
        {
            buildingListContent = FindContentInScrollView(buildingScrollView);

            if (buildingListContent != null)
            {
                Debug.Log($"[HBSSampleHousePanel] Auto-found buildingListContent in ScrollView structure");
            }
        }

        if (buildingListContent == null)
        {
            Debug.LogWarning("[HBSSampleHousePanel] buildingListContent is not assigned and could not be auto-detected");
            return;
        }

        // Content에 Vertical Layout Group 자동 추가 (없는 경우)
        EnsureLayoutGroup();

        buildingSlots.Clear();

        // Content 하위의 모든 자식을 슬롯으로 추가
        for (int i = 0; i < buildingListContent.childCount; i++)
        {
            GameObject slot = buildingListContent.GetChild(i).gameObject;
            buildingSlots.Add(slot);
            slot.SetActive(false); // 초기에는 모두 비활성화
        }

        Debug.Log($"[HBSSampleHousePanel] Initialized {buildingSlots.Count} building slots");

        UpdateInfo(currentHouse);
    }

    /// <summary>
    /// Content에 Vertical Layout Group이 있는지 확인하고 없으면 추가
    /// </summary>
    private void EnsureLayoutGroup()
    {
        if (buildingListContent == null) return;

        UnityEngine.UI.VerticalLayoutGroup layoutGroup = buildingListContent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();

        if (layoutGroup == null)
        {
            layoutGroup = buildingListContent.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();

            // 기본 설정
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.spacing = 0f;

            Debug.Log($"[HBSSampleHousePanel] Added VerticalLayoutGroup to buildingListContent");
        }
    }

    /// <summary>
    /// ScrollView의 기본 구조(ScrollView - Viewport - Content)에서 Content 찾기
    /// </summary>
    private RectTransform FindContentInScrollView(GameObject scrollView)
    {
        if (scrollView == null) return null;

        // ScrollView 직접 하위에서 "Viewport" 찾기
        Transform viewport = scrollView.transform.Find("Viewport");
        if (viewport != null)
        {
            // Viewport 하위에서 "Content" 찾기
            Transform content = viewport.Find("Content");
            if (content != null)
            {
                return content.GetComponent<RectTransform>();
            }
        }

        // 대소문자 구분 없이 검색 시도
        foreach (Transform child in scrollView.transform)
        {
            if (child.name.ToLower() == "viewport")
            {
                foreach (Transform grandChild in child)
                {
                    if (grandChild.name.ToLower() == "content")
                    {
                        return grandChild.GetComponent<RectTransform>();
                    }
                }
            }
        }

        Debug.LogWarning($"[HBSSampleHousePanel] Could not find Content in ScrollView '{scrollView.name}'. Expected structure: ScrollView/Viewport/Content");
        return null;
    }

    /// <summary>
    /// House 정보를 UI에 업데이트
    /// </summary>
    public void UpdateInfo(House house)
    {
        if (house == null)
        {
            Debug.LogWarning("HBSSampleHousePanel: House is null");
            return;
        }

        currentHouse = house;

        // Basic Info
        if (houseName != null)
            houseName.text = house.HouseType.DisplayTypeName;

        if (houseType != null)
            houseType.text = house.HouseType.ToString();

        // Status
        if (residents != null)
            residents.text = $"거주민: {house.CurrentResidents}/{house.MaxResidents}";

        if (durability != null)
            durability.text = $"내구도: {house.Durability:F1}%";

        if (durabilitySlider != null)
            durabilitySlider.value = house.Durability / 100f;

        if (usageState != null)
            usageState.text = $"상태: {house.UsageState}";

        // Storage
        if (storage != null)
            storage.text = $"저장소: {house.CurrentStorageUsed}/{house.StorageCapacity}";

        // Building List
        UpdateBuildingList(house);
    }

    /// <summary>
    /// Building 목록 ScrollView 업데이트
    /// 활성화된 슬롯 수에 맞춰 Content 높이 조정
    /// </summary>
    private void UpdateBuildingList(House house)
    {
        if (house == null || buildingListContent == null || buildingSlots.Count == 0)
            return;

        // 모든 슬롯 비활성화 및 클리어
        foreach (var slot in buildingSlots)
        {
            slot.SetActive(false);

            // HBSSampleBuildingSlot이 있으면 클리어
            HBSSampleBuildingSlot slotComponent = slot.GetComponent<HBSSampleBuildingSlot>();
            if (slotComponent != null)
            {
                slotComponent.Clear();
            }
        }

        // 빌딩 개수만큼 슬롯 활성화 및 정보 표시
        int buildingCount = house.Buildings.Count;
        for (int i = 0; i < buildingCount && i < buildingSlots.Count; i++)
        {
            GameObject slot = buildingSlots[i];
            Building building = house.Buildings[i];

            slot.SetActive(true);

            // HBSSampleBuildingSlot 컴포넌트를 통해 Building 정보 설정
            HBSSampleBuildingSlot slotComponent = slot.GetComponent<HBSSampleBuildingSlot>();
            if (slotComponent != null && building != null)
            {
                slotComponent.SetBuilding(building);

                // 슬롯 클릭 이벤트 구독 (중복 구독 방지)
                slotComponent.OnSlotClicked -= HandleBuildingSlotClicked;
                slotComponent.OnSlotClicked += HandleBuildingSlotClicked;
            }
            else
            {
                // 폴백: 컴포넌트가 없으면 직접 TextMeshProUGUI 업데이트
                TextMeshProUGUI slotText = slot.GetComponentInChildren<TextMeshProUGUI>();
                if (slotText != null && building != null)
                {
                    slotText.text = building.name;
                }
            }
        }

        // Content 높이 조정 (VerticalLayoutGroup 여부와 관계없이 항상 수동 설정)
        int activeSlotCount = Mathf.Min(buildingCount, buildingSlots.Count);
        float totalHeight = 0f;

        // 실제 슬롯 높이 가져오기 (buildingSlots가 있으면 첫 번째 슬롯의 높이 사용)
        float slotHeight = buildingSlotHeight; // 기본값
        if (buildingSlots.Count > 0 && buildingSlots[0] != null)
        {
            RectTransform slotRect = buildingSlots[0].GetComponent<RectTransform>();
            if (slotRect != null)
            {
                slotHeight = slotRect.rect.height;
            }
        }

        // VerticalLayoutGroup이 있으면 spacing 고려
        VerticalLayoutGroup layoutGroup = buildingListContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            // 각 슬롯 높이 + spacing
            totalHeight = activeSlotCount * slotHeight;
            if (activeSlotCount > 1)
            {
                totalHeight += layoutGroup.spacing * (activeSlotCount - 1);
            }

            // LayoutGroup의 padding 고려
            totalHeight += layoutGroup.padding.top + layoutGroup.padding.bottom;
        }
        else
        {
            // LayoutGroup이 없으면 단순 계산
            totalHeight = activeSlotCount * slotHeight;
        }

        // Content 높이 설정
        buildingListContent.sizeDelta = new Vector2(buildingListContent.sizeDelta.x, totalHeight);
    }

    /// <summary>
    /// 패널을 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        currentHouse = null;
    }

    #region Button Handlers
    /// <summary>
    /// [Button] 리모델링 모드 진입
    /// </summary>
    public void OnClickRemodelingButton()
    {
        if (buildingSystem != null && currentHouse != null)
        {
            buildingSystem.SetState(SystemState.Remodeling);
        }
    }

    /// <summary>
    /// [Button] 리모델링 취소 - 백업된 상태로 복원
    /// </summary>
    public void OnClickCancelButton()
    {
        if (buildingSystem != null && buildingSystem.RemodelingController != null)
        {
            // 리모델링 취소 - 백업된 상태로 복원
            bool success = buildingSystem.RemodelingController.CancelRemodeling();

            if (success)
            {
                buildingSystem.SetState(SystemState.Off);
                Debug.Log("[HBSSampleHousePanel] Remodeling cancelled successfully.");
            }
        }
    }

    /// <summary>
    /// [Button] 리모델링 완료 - House UnderConstruction, 변경된 Building 0단계로 초기화
    /// </summary>
    public void OnClickConfirmButton()
    {
        if (buildingSystem != null && buildingSystem.RemodelingController != null)
        {
            // 리모델링 완성 - 하우스 UnderConstruction, 변경된 빌딩 0단계로 초기화
            bool success = buildingSystem.RemodelingController.CompleteRemodeling();

            if (success)
            {
                buildingSystem.SetState(SystemState.Off);
                Debug.Log("[HBSSampleHousePanel] Remodeling completed successfully.");
            }
        }
    }

    /// <summary>
    /// [Button] 리모델링 중 Building 추가 시작
    /// Inspector에서 지정한 buildingPrefabToAdd를 드래그 모드로 배치합니다.
    /// - 좌클릭: 배치 확정 (하우스에 추가)
    /// - 우클릭: 취소 (카탈로그에 반환)
    /// </summary>
    public void OnClickAddBuilding()
    {
        if (buildingSystem == null || buildingSystem.RemodelingController == null)
        {
            Debug.LogWarning("[HBSSampleHousePanel] Building system or remodeling controller is null.");
            return;
        }

        if (buildingPrefabToAdd == null)
        {
            Debug.LogWarning("[HBSSampleHousePanel] Building prefab to add is not assigned. Please assign it in the Inspector.");
            return;
        }

        if (currentHouse == null)
        {
            Debug.LogWarning("[HBSSampleHousePanel] No house selected.");
            return;
        }

        // RemodelingController를 통해 Building 추가 시작 (드래그 모드 진입)
        Building newBuilding = buildingSystem.RemodelingController.AddBuildingDuringRemodeling(buildingPrefabToAdd);

        if (newBuilding != null)
        {
            Debug.Log($"[HBSSampleHousePanel] Started adding building '{newBuilding.name}'. Drag to position, left-click to place, right-click to cancel.");
        }

        // 지우개 모드 해제
        SetEraserMode(false);
    }

    /// <summary>
    /// [Button] 지우개 모드 토글
    /// 지우개 모드에서 Building을 클릭하면 철거됩니다 (필수 건물 제외)
    /// </summary>
    public void OnClickEraser()
    {
        if (buildingSystem == null || buildingSystem.RemodelingController == null)
        {
            Debug.LogWarning("[HBSSampleHousePanel] Building system or remodeling controller is null.");
            return;
        }

        if (currentHouse == null)
        {
            Debug.LogWarning("[HBSSampleHousePanel] No house selected.");
            return;
        }

        // 지우개 모드 토글
        SetEraserMode(!isEraserMode);
    }

    /// <summary>
    /// 지우개 모드 상태 설정 및 UI 업데이트
    /// </summary>
    private void SetEraserMode(bool enabled)
    {
        isEraserMode = enabled;

        // RemodelingController에도 지우개 모드 상태 전달
        if (buildingSystem?.RemodelingController != null)
        {
            buildingSystem.RemodelingController.SetEraserMode(enabled);
        }

        // 지우개 버튼 색상 변경 (Button의 ColorBlock을 수정해서 상태 색상이 덮어쓰지 않도록)
        if (eraserButton != null)
        {
            ColorBlock colorBlock = eraserButton.colors;
            Color targetColor = isEraserMode ? eraserActiveColor : eraserInactiveColor;

            // 모든 상태의 색상을 동일하게 설정하여 버튼 상태 변화에도 색상 유지
            colorBlock.normalColor = targetColor;
            colorBlock.highlightedColor = targetColor;
            colorBlock.pressedColor = targetColor;
            colorBlock.selectedColor = targetColor;

            eraserButton.colors = colorBlock;
        }
        else if (eraserButtonImage != null)
        {
            // eraserButton이 없으면 Image 색상만 변경 (폴백)
            eraserButtonImage.color = isEraserMode ? eraserActiveColor : eraserInactiveColor;
        }

        if (isEraserMode)
        {
            Debug.Log("[HBSSampleHousePanel] Eraser mode enabled. Click a building to remove it (required buildings cannot be removed).");
        }
        else
        {
            Debug.Log("[HBSSampleHousePanel] Eraser mode disabled.");
        }
    }

    /// <summary>
    /// [Button - Debug] 현재 하우스의 모든 빌딩에 필요한 자원을 완납
    /// </summary>
    public void OnClickFillAllResources()
    {
        if (currentHouse == null)
        {
            Debug.LogWarning("[HBSSampleHousePanel] No house selected.");
            return;
        }

        int buildingCount = 0;
        int stageCount = 0;

        foreach (var building in currentHouse.Buildings)
        {
            if (building == null || building.IsCompleted) continue;

            buildingCount++;

            // 현재 단계의 필요 자원 가져오기
            Cost[] requiredResources = building.GetCurrentStageRequiredResources();

            if (requiredResources == null || requiredResources.Length == 0)
            {
                Debug.Log($"[Debug] {building.name} Stage {building.CurrentStageIndex}: No required resources");
                continue;
            }

            // 자원 컴포넌트가 있는 경우에만 자원 추가
            ConstructionResourceComponent resourceComp = building.GetComponent<ConstructionResourceComponent>();
            if (resourceComp == null)
            {
                Debug.LogWarning($"[Debug] {building.name} has no ConstructionResourceComponent. Skipping resource fill.");
                continue;
            }

            // 각 필요 자원을 대기 자원에 완납
            foreach (var cost in requiredResources)
            {
                if (cost.ResourceType == null) continue;

                int currentAmount = resourceComp.GetCollectedAmount(cost.ResourceType);
                int needed = cost.Amount - currentAmount;

                if (needed > 0)
                {
                    resourceComp.AddPendingResource(cost.ResourceType, needed);
                    Debug.Log($"[Debug] {building.name} Stage {building.CurrentStageIndex}: Added {cost.ResourceType.name} x{needed} to pending resources");
                }
            }

            stageCount++;
        }

        Debug.Log($"[HBSSampleHousePanel] Filled resources for {stageCount} stages in {buildingCount} buildings");

        // UI 갱신
        UpdateInfo(currentHouse);
    }
    #endregion

    #region Event Handlers
    // private void HandleHouseSelected(House house)
    // {
    //     RemodelingToInformationMode();
    //     UpdateInfo(house);
    // }

    private void HandleRemodelingStarted(House house)
    {
        if (house == currentHouse)
        {
            InformationToRemodelingMode();
            UpdateInfo(currentHouse);
        }
    }

    private void HandleRemodelingCompleted(House house)
    {
        if (house == currentHouse)
        {
            RemodelingToInformationMode();
            SetEraserMode(false); // 지우개 모드 해제
            UpdateInfo(currentHouse);
        }
    }

    private void HandleRemodelingCancelled(House house)
    {
        if (house == currentHouse)
        {
            RemodelingToInformationMode();
            SetEraserMode(false); // 지우개 모드 해제
            UpdateInfo(currentHouse);
        }
    }

    private void HandleBuildingModified(House house, Building building)
    {
        if (house == currentHouse)
        {
            // Building 목록이 변경되었으므로 업데이트
            UpdateBuildingList(currentHouse);
        }
    }

    private void HandleBuildingSlotClicked(Building building)
    {
        if (building == null || currentHouse == null)
            return;

        // 지우개 모드가 활성화되어 있으면 건물 제거
        if (isEraserMode)
        {
            if (buildingSystem == null || buildingSystem.RemodelingController == null)
            {
                Debug.LogWarning("[HBSSampleHousePanel] Cannot remove building: RemodelingController is not available.");
                return;
            }

            bool removed = buildingSystem.RemodelingController.RemoveBuildingDuringRemodeling(building, currentHouse);

            if (removed)
            {
                Debug.Log($"[HBSSampleHousePanel] Successfully removed building: {building.name}");
                // 지우개 모드 유지 (계속 지울 수 있도록)
            }
            else
            {
                Debug.LogWarning($"[HBSSampleHousePanel] Failed to remove building: {building.name}");
            }
        }
        else
        {
            // 지우개 모드가 아닐 때의 동작 (필요시 확장)
            Debug.Log($"[HBSSampleHousePanel] Building clicked: {building.name}");
        }
    }
    #endregion

    #region UI Mode Switching
    private void InformationToRemodelingMode()
    {
        if (informationPanel != null)
            informationPanel.SetActive(false);

        if (remodelingPanel != null)
            remodelingPanel.SetActive(true);
    }

    private void RemodelingToInformationMode()
    {
        if (informationPanel != null)
            informationPanel.SetActive(true);

        if (remodelingPanel != null)
            remodelingPanel.SetActive(false);
    }
    #endregion

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (buildingSystem != null)
        {
            buildingSystem.Events.OnRemodelingStarted -= HandleRemodelingStarted;
            buildingSystem.Events.OnRemodelingCompleted -= HandleRemodelingCompleted;
            buildingSystem.Events.OnRemodelingCancelled -= HandleRemodelingCancelled;
            buildingSystem.Events.OnBuildingModified -= HandleBuildingModified;
        }
    }
}
