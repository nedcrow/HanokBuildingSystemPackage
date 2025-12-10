using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// HanokBuildingSystem 상태 변경 버튼
    /// Unity Button의 OnClick 이벤트에서 호출되는 메서드를 제공합니다.
    /// </summary>
    public class HBSButton : MonoBehaviour
    {
        private HanokBuildingSystem buildingSystem;

        void Start()
        {
            buildingSystem = HanokBuildingSystem.Instance;
        }

        #region Button Click Handlers (Unity Event에서 호출)
        public void OnClickedHouseButton()
        {
            if (buildingSystem == null)
                buildingSystem = HanokBuildingSystem.Instance;

            // 토글 방식: Off <-> NewBuilding
            buildingSystem.SetState(
                buildingSystem.CurrentState == SystemState.Off
                ? SystemState.NewBuilding
                : SystemState.Off
            );
        }

        public void OnStateOff()
        {
            if (buildingSystem == null)
                buildingSystem = HanokBuildingSystem.Instance;

            buildingSystem.SetState(SystemState.Off);
        }

        public void OnStateNewBuilding()
        {
            if (buildingSystem == null)
                buildingSystem = HanokBuildingSystem.Instance;

            buildingSystem.SetState(SystemState.NewBuilding);
        }

        public void OnStateRemodeling()
        {
            if (buildingSystem == null)
                buildingSystem = HanokBuildingSystem.Instance;

            buildingSystem.SetState(SystemState.Remodeling);
        }
        #endregion
    }
}
