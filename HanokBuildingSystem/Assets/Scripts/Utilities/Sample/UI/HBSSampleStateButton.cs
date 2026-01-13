using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// [Sample] HanokBuildingSystem 상태 변경 버튼
    ///
    /// 샘플 용도:
    /// - HanokBuildingSystem의 상태(Off, NewBuilding, Remodeling) 변경 데모
    /// - Unity Button의 OnClick 이벤트에서 호출되는 메서드 제공
    /// - 시스템 상태 전환 워크플로우 예제
    ///
    /// 사용 방법:
    /// 1. UI Button 게임오브젝트에 이 컴포넌트 추가
    /// 2. Button의 OnClick 이벤트에서 원하는 메서드 연결
    ///    - OnClickedHouseButton(): Off ↔ NewBuilding 토글
    ///    - OnStateOff(): Off 상태로 전환
    ///    - OnStateNewBuilding(): NewBuilding 상태로 전환
    ///    - OnStateRemodeling(): Remodeling 상태로 전환
    /// </summary>
    public class HBSSampleStateButton : MonoBehaviour
    {
        private HanokBuildingSystem buildingSystem;

        void Start()
        {
            buildingSystem = HanokBuildingSystem.Instance;
        }

        #region Button Click Handlers (Unity Event에서 호출)
        /// <summary>
        /// Off ↔ NewBuilding 토글 버튼
        /// </summary>
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

        /// <summary>
        /// 시스템을 Off 상태로 전환
        /// </summary>
        public void OnStateOff()
        {
            if (buildingSystem == null)
                buildingSystem = HanokBuildingSystem.Instance;

            buildingSystem.SetState(SystemState.Off);
        }

        /// <summary>
        /// 시스템을 NewBuilding 상태로 전환
        /// </summary>
        public void OnStateNewBuilding()
        {
            if (buildingSystem == null)
                buildingSystem = HanokBuildingSystem.Instance;

            buildingSystem.SetState(SystemState.NewBuilding);
        }

        /// <summary>
        /// 시스템을 Remodeling 상태로 전환
        /// </summary>
        public void OnStateRemodeling()
        {
            if (buildingSystem == null)
                buildingSystem = HanokBuildingSystem.Instance;

            buildingSystem.SetState(SystemState.Remodeling);
        }
        #endregion
    }
}
