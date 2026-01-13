using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// 사용 예시:
    /// <code>
    /// // 게임 프로젝트에서 구독
    /// void Start()
    /// {
    ///     var system = HanokBuildingSystem.Instance;
    ///     system.Events.OnHouseSelected += ShowHouseUI;
    ///     system.Events.OnPlotCreated += PlayPlotSound;
    /// }
    ///
    /// void ShowHouseUI(House house)
    /// {
    ///     // 게임별 UI 표시 로직
    ///     HouseInfoPanel.Instance.Show(house);
    /// }
    /// </code>
    /// </summary>
    public class HanokBuildingSystemEvents
    {
        #region State Events
        public event Action<SystemState, SystemState> OnStateChanged;
        #endregion

        #region House Selection Events
        public event Action<House> OnHouseSelected;
        public event Action<House> OnHouseDeselected;
        public event Action<List<House>> OnSelectionClearing;
        public event Action OnSelectionCleared;
        public event Action<House> OnHouseHoverEnter;
        public event Action<House> OnHouseHoverExit;
        #endregion

        #region Plot Events
        public event Action<Plot> OnPlotCreated;
        public event Action<Plot> OnPlotCompleted;
        public event Action<Plot, Vector3> OnVertexAdded;
        public event Action<Plot, int> OnVertexRemoved;
        #endregion

        #region Construction Events
        public event Action<House> OnConstructionStarted;
        public event Action<Building> OnBuildingConstructed;
        public event Action<House> OnConstructionCancelled;
        #endregion

        #region Remodeling Events
        public event Action<House> OnRemodelingStarted;
        public event Action<House, Building> OnBuildingAddedToHouse;
        public event Action<House, Building> OnBuildingRemovedFromHouse;
        public event Action<House, Building> OnBuildingModified;
        public event Action<House> OnRemodelingCompleted;
        public event Action<House> OnRemodelingCancelled;
        #endregion

        #region Internal Invoke Methods
        // Core 시스템에서만 호출하는 메서드들
        internal void RaiseStateChanged(SystemState oldState, SystemState newState)
            => OnStateChanged?.Invoke(oldState, newState);

        internal void RaiseHouseSelected(House house)
            => OnHouseSelected?.Invoke(house);

        internal void RaiseHouseDeselected(House house)
            => OnHouseDeselected?.Invoke(house);

        internal void RaiseSelectionClearing(List<House> houses)
            => OnSelectionClearing?.Invoke(houses);

        internal void RaiseSelectionCleared()
            => OnSelectionCleared?.Invoke();

        internal void RaiseHouseHoverEnter(House house)
            => OnHouseHoverEnter?.Invoke(house);

        internal void RaiseHouseHoverExit(House house)
            => OnHouseHoverExit?.Invoke(house);

        internal void RaisePlotCreated(Plot plot)
            => OnPlotCreated?.Invoke(plot);

        internal void RaisePlotCompleted(Plot plot)
            => OnPlotCompleted?.Invoke(plot);

        internal void RaiseVertexAdded(Plot plot, Vector3 position)
            => OnVertexAdded?.Invoke(plot, position);

        internal void RaiseVertexRemoved(Plot plot, int index)
            => OnVertexRemoved?.Invoke(plot, index);

        internal void RaiseConstructionStarted(House house)
            => OnConstructionStarted?.Invoke(house);

        internal void RaiseBuildingConstructed(Building building)
            => OnBuildingConstructed?.Invoke(building);

        internal void RaiseConstructionCancelled(House house)
            => OnConstructionCancelled?.Invoke(house);

        internal void RaiseRemodelingStarted(House house)
            => OnRemodelingStarted?.Invoke(house);

        internal void RaiseBuildingAddedToHouse(House house, Building building)
            => OnBuildingAddedToHouse?.Invoke(house, building);

        internal void RaiseBuildingRemovedFromHouse(House house, Building building)
            => OnBuildingRemovedFromHouse?.Invoke(house, building);

        internal void RaiseBuildingModified(House house, Building building)
            => OnBuildingModified?.Invoke(house, building);

        internal void RaiseRemodelingCompleted(House house)
            => OnRemodelingCompleted?.Invoke(house);

        internal void RaiseRemodelingCancelled(House house)
            => OnRemodelingCancelled?.Invoke(house);
        #endregion
    }
}
