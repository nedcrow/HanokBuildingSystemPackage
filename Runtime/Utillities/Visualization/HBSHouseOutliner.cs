using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(House))]
    public sealed class HBSHouseOutliner : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("하우스 선택/배치 상태가 바뀔 때, 자식 빌딩들의 Outliner도 같이 제어할지")]
        [SerializeField] private bool includeChildBuildings = true;

        [Tooltip("자식 빌딩 목록이 바뀌면 외부에서 Setup()/RebuildTargets()를 호출해 주세요.")]
        [SerializeField] private bool autoRebuildOnEnable = true;

        private readonly List<IPlacementFeedback> _targets = new();
        private PlacementVisualState _current = PlacementVisualState.None;

        private void OnEnable()
        {
            if (autoRebuildOnEnable)
                RebuildTargets();
        }

        /// <summary>
        /// House 내부 빌딩 추가/제거/수정 후 호출용.
        /// Setup이라는 이름이 의도(초기 구성/재구성)에 가장 잘 맞음.
        /// </summary>
        public void Setup()
        {
            RebuildTargets();

            // 현재 상태가 켜져 있는 중이라면, 새로 들어온 대상에도 즉시 반영
            if (_current != PlacementVisualState.None)
                BroadcastState(_current);
        }

        /// <summary>현재 하우스가 제어할 피드백 대상 목록 재구성</summary>
        public void RebuildTargets()
        {
            _targets.Clear();

            if (!includeChildBuildings) return;

            // 하우스 아래의 Building Outliner들을 모음
            // (HouseOutliner 자기 자신은 제외)
            var outliners = GetComponentsInChildren<HBSBuildingOutliner>(true);
            foreach (var o in outliners)
            {
                if (o == null) continue;
                // 혹시 하우스 루트에 붙어있을 가능성 방지
                if (ReferenceEquals(o.gameObject, this.gameObject)) continue;

                _targets.Add(o);
            }
        }

        public void SetPlacementState(PlacementVisualState state)
        {
            if (state == _current) return;
            _current = state;

            BroadcastState(state);
        }

        public void ClearPlacementState()
        {
            _current = PlacementVisualState.None;

            for (int i = 0; i < _targets.Count; i++)
                _targets[i]?.ClearPlacementState();
        }

        private void BroadcastState(PlacementVisualState state)
        {
            for (int i = 0; i < _targets.Count; i++)
                _targets[i]?.SetPlacementState(state);
        }
    }
}
