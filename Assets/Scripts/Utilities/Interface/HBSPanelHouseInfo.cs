using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HanokBuildingSystem;
using System.Collections.Generic;
using System;

/// <summary>
/// House 정보를 표시하는 UI 패널
/// Panel_HouseInfo 프리팹에 붙여서 사용
/// </summary>
public class HBSPanelHouseInfo : MonoBehaviour
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

    private House currentHouse;
    private HanokBuildingSystem.HanokBuildingSystem buildingSystem;

    private List<Building> modifiedBuildings;
    private List<Building> addedBuildings;

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
            Debug.LogWarning("Panel_HouseInfo: House is null");
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

    public void RefreshInfo()
    {
        if (currentHouse != null)
        {
            UpdateInfo(currentHouse);
        }
    }

    public void OnClickRemodelingButton()
    {
        if (buildingSystem != null && currentHouse != null)
        {
            buildingSystem.SetState(SystemState.Remodeling);
        }
    }

    public void OnClickCancelButton()
    {
        if (buildingSystem != null && buildingSystem.RemodelingController != null)
        {
            // 리모델링 취소 - 백업된 상태로 복원
            bool success = buildingSystem.RemodelingController.CancelRemodeling();

            if (success)
            {
                buildingSystem.SetState(SystemState.Off);
                Debug.Log("[HBSPanelHouseInfo] Remodeling cancelled successfully.");
            }
        }
    }

    public void OnClickConfirmButton()
    {
        if (buildingSystem != null && buildingSystem.RemodelingController != null)
        {
            // 리모델링 완성 - 하우스 UnderConstruction, 변경된 빌딩 0단계로 초기화
            bool success = buildingSystem.RemodelingController.CompleteRemodeling();

            if (success)
            {
                buildingSystem.SetState(SystemState.Off);
                Debug.Log("[HBSPanelHouseInfo] Remodeling completed successfully.");
            }
        }
    }

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

    private void InformationToRemodelingMode()
    {
        informationPanel.SetActive(false);
        remodelingPanel.SetActive(true);
    }
    private void RemodelingToInformationMode()
    {
        informationPanel.SetActive(true);
        remodelingPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (buildingSystem != null)
        {
            buildingSystem.Events.OnRemodelingStarted -= HandleRemodelingStarted;
            buildingSystem.Events.OnRemodelingCompleted -= HandleRemodelingCompleted;
            buildingSystem.Events.OnRemodelingCancelled -= HandleRemodelingCancelled;
        }
    }
}
