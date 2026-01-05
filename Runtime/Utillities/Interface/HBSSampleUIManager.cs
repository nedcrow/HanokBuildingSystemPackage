using UnityEngine;
using HanokBuildingSystem;
using System.Collections.Generic;

/// <summary>
/// [Sample] House 관련 UI 표시/숨김 관리
///
/// 샘플 용도:
/// - HanokBuildingSystem 이벤트 구독을 통한 UI 반응 데모
/// - House 선택/해제 시 UI 패널 표시/숨김 예제
/// - 시스템 상태 변경 시 UI 동기화 워크플로우 예제
///
/// 사용 방법:
/// 1. 씬의 빈 GameObject에 이 컴포넌트 추가
/// 2. Inspector에서 HBSSampleHousePanel을 houseInfoPanel에 할당
/// 3. HanokBuildingSystem 이벤트가 자동으로 연결되어 UI 관리
///
/// 이벤트 연동:
/// - OnHouseSelected: House 선택 시 패널 표시
/// - OnHouseDeselected: House 해제 시 패널 숨김 (선택된 House가 없을 때만)
/// - OnSelectionClearing: 모든 선택 해제 시 패널 숨김
/// - OnStateChanged: NewBuilding 모드 전환 시 패널 숨김
/// </summary>
public class HBSSampleUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private HBSSampleHousePanel houseInfoPanel;

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
            buildingSystem.Events.OnSelectionClearing += HandleSelectionCleared;

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
            buildingSystem.Events.OnSelectionClearing -= HandleSelectionCleared;
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

    private void HandleSelectionCleared(List<House> houses)
    {
        HideAllInfo();
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
