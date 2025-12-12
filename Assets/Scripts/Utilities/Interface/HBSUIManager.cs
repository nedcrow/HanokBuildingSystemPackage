using UnityEngine;
using HanokBuildingSystem;

/// <summary>
/// House 관련 UI를 관리하는 클래스
/// HanokBuildingSystem의 이벤트를 구독하여 UI를 표시/숨김
/// </summary>
public class HBSUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private HBSPanelHouseInfo houseInfoPanel;

    private HanokBuildingSystem.HanokBuildingSystem buildingSystem;

    void Start()
    {
        buildingSystem = HanokBuildingSystem.HanokBuildingSystem.Instance;

        // 초기 상태: 패널 숨김
        if (houseInfoPanel != null)
        {
            OnEnable();
            houseInfoPanel.Hide();
        }
    }

    void OnEnable()
    {
        if (buildingSystem != null)
        {
            // House 선택 시 정보 표시
            buildingSystem.Events.OnHouseSelected += ShowHouseInfo;

            // 선택 해제 시 숨김
            buildingSystem.Events.OnHouseDeselected += HideHouseInfo;
            buildingSystem.Events.OnSelectionCleared += HideAllInfo;

            // 상태 변경 시 처리
            buildingSystem.Events.OnStateChanged += OnSystemStateChanged;
        }
    }

    void OnDisable()
    {
        if (buildingSystem != null)
        {
            buildingSystem.Events.OnHouseSelected -= ShowHouseInfo;
            buildingSystem.Events.OnHouseDeselected -= HideHouseInfo;
            buildingSystem.Events.OnSelectionCleared -= HideAllInfo;
            buildingSystem.Events.OnStateChanged -= OnSystemStateChanged;
        }
    }

    private void ShowHouseInfo(House house)
    {
        if (house == null || houseInfoPanel == null) return;

        // 패널에 정보 업데이트 후 표시
        houseInfoPanel.gameObject.SetActive(true);
        houseInfoPanel.UpdateInfo(house);
    }

    private void HideHouseInfo(House house)
    {
        // 선택된 House가 없으면 패널 숨김
        if (houseInfoPanel != null && buildingSystem.CurrentHouses.Count == 0)
        {
            houseInfoPanel.Hide();
        }
    }

    private void HideAllInfo()
    {
        if (houseInfoPanel != null)
        {
            houseInfoPanel.Hide();
        }
    }

    private void OnSystemStateChanged(SystemState oldState, SystemState newState)
    {
        // NewBuilding 모드로 전환 시 패널 숨김
        if (newState == SystemState.NewBuilding)
        {
            HideAllInfo();
        }
    }
}
