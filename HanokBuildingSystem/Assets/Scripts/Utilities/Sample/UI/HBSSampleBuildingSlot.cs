using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HanokBuildingSystem;
using System;

/// <summary>
/// [Sample] Building 정보를 표시하는 UI 슬롯
///
/// 샘플 용도:
/// - ScrollView에서 Building 목록을 표시하는 개별 슬롯 예제
/// - Building 이름 표시
/// - 클릭 이벤트를 통한 Building 선택 기능 예제
///
/// 사용 방법:
/// 1. Panel_BuildingSlot GameObject에 이 컴포넌트 추가
/// 2. Inspector에서 buildingNameText에 TextMeshProUGUI 할당
/// 3. 코드에서 SetBuilding(Building) 호출하여 정보 표시
/// 4. OnSlotClicked 이벤트 구독으로 클릭 처리
///
/// 구조:
/// Panel_BuildingSlot
/// ├── Button (클릭 감지)
/// └── TextMeshProUGUI (buildingNameText)
/// </summary>
public class HBSSampleBuildingSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private Button slotButton;

    private Building currentBuilding;

    // 슬롯 클릭 이벤트
    public event Action<Building> OnSlotClicked;

    private void Start()
    {
        // Button이 없으면 자동으로 추가
        if (slotButton == null)
        {
            slotButton = GetComponent<Button>();
            if (slotButton == null)
            {
                slotButton = gameObject.AddComponent<Button>();
            }
        }

        // Button 클릭 이벤트 등록
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(HandleSlotClicked);
        }
    }

    private void HandleSlotClicked()
    {
        if (currentBuilding != null)
        {
            OnSlotClicked?.Invoke(currentBuilding);
        }
    }

    /// <summary>
    /// Building 정보를 슬롯에 설정하고 표시
    /// </summary>
    public void SetBuilding(Building building)
    {
        currentBuilding = building;
        UpdateDisplay();
    }

    /// <summary>
    /// 현재 Building 정보 가져오기
    /// </summary>
    public Building GetBuilding()
    {
        return currentBuilding;
    }

    /// <summary>
    /// Building 정보를 텍스트에 표시
    /// </summary>
    private void UpdateDisplay()
    {
        if (buildingNameText == null)
        {
            Debug.LogWarning("[HBSSampleBuildingSlot] buildingNameText is not assigned");
            return;
        }

        if (currentBuilding == null)
        {
            buildingNameText.text = "<Empty>";
            return;
        }

        // Building 이름만 표시
        buildingNameText.text = currentBuilding.StatusData.BuildingType.DisplayTypeName;
    }

    /// <summary>
    /// 슬롯 클리어
    /// </summary>
    public void Clear()
    {
        currentBuilding = null;

        if (buildingNameText != null)
        {
            buildingNameText.text = string.Empty;
        }
    }

    /// <summary>
    /// 슬롯이 비어있는지 확인
    /// </summary>
    public bool IsEmpty()
    {
        return currentBuilding == null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// [Editor] buildingNameText 자동 할당 시도
    /// </summary>
    private void Reset()
    {
        if (buildingNameText == null)
        {
            buildingNameText = GetComponentInChildren<TextMeshProUGUI>();
            if (buildingNameText != null)
            {
                Debug.Log($"[HBSSampleBuildingSlot] Auto-assigned buildingNameText: {buildingNameText.name}");
            }
        }
    }
#endif
}
