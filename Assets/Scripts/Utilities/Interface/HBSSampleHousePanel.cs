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
    [Tooltip("리모델링 중 추가할 Building 프리팹을 지정하세요 (GameObject)")]

    private House currentHouse;
    private HanokBuildingSystem.HanokBuildingSystem buildingSystem;

    void Start()
    {
        if (buildingSystem == null)
        {
            buildingSystem = HanokBuildingSystem.HanokBuildingSystem.Instance;

            buildingSystem.Events.OnHouseSelected += HandleHouseSelected;

            buildingSystem.Events.OnRemodelingStarted += HandleRemodelingStarted;
            buildingSystem.Events.OnRemodelingCompleted += HandleRemodelingCompleted;
            buildingSystem.Events.OnRemodelingCancelled += HandleRemodelingCancelled;
        }
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
            houseName.text = house.name;

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
    }

    /// <summary>
    /// 패널을 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        currentHouse = null;
    }

    /// <summary>
    /// 현재 House 정보를 다시 읽어 UI 갱신
    /// </summary>
    public void RefreshInfo()
    {
        if (currentHouse != null)
        {
            UpdateInfo(currentHouse);
        }
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

            // 각 필요 자원을 대기 자원에 완납
            foreach (var cost in requiredResources)
            {
                if (cost.ResourceType == null) continue;

                int currentAmount = building.GetCollectedAmount(cost.ResourceType);
                int needed = cost.Amount - currentAmount;

                if (needed > 0)
                {
                    building.AddPendingResource(cost.ResourceType, needed);
                    Debug.Log($"[Debug] {building.name} Stage {building.CurrentStageIndex}: Added {cost.ResourceType.name} x{needed} to pending resources");
                }
            }

            stageCount++;
        }

        Debug.Log($"[HBSSampleHousePanel] Filled resources for {stageCount} stages in {buildingCount} buildings");

        // UI 갱신
        RefreshInfo();
    }
    #endregion

    #region Event Handlers
    private void HandleHouseSelected(House house)
    {
        RemodelingToInformationMode();
        UpdateInfo(house);
    }

    private void HandleRemodelingStarted(House house)
    {
        if (house == currentHouse)
        {
            InformationToRemodelingMode();
            RefreshInfo();
        }
    }

    private void HandleRemodelingCompleted(House house)
    {
        if (house == currentHouse)
        {
            RemodelingToInformationMode();
            RefreshInfo();
        }
    }

    private void HandleRemodelingCancelled(House house)
    {
        if (house == currentHouse)
        {
            RemodelingToInformationMode();
            RefreshInfo();
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
            buildingSystem.Events.OnHouseSelected -= HandleHouseSelected;
            buildingSystem.Events.OnRemodelingStarted -= HandleRemodelingStarted;
            buildingSystem.Events.OnRemodelingCompleted -= HandleRemodelingCompleted;
            buildingSystem.Events.OnRemodelingCancelled -= HandleRemodelingCancelled;
        }
    }
}
