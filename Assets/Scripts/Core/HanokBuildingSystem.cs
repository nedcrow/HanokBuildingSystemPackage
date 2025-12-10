using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    public enum SystemState
    {
        Off,
        NewBuilding,
        Remodeling
    }

    public class HanokBuildingSystem : MonoBehaviour
    {
        #region Singleton
        private static HanokBuildingSystem instance;
        public static HanokBuildingSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<HanokBuildingSystem>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("HanokBuildingSystem");
                        instance = go.AddComponent<HanokBuildingSystem>();
                    }
                }
                return instance;
            }
        }
        #endregion

        #region Components
        [Header("Required Components")]
        [SerializeField] private PlotController plotController;
        [SerializeField] private RemodelingController remodelingController;

        [Header("Catalogs")]
        [SerializeField] private HouseCatalog houseCatalog;
        [SerializeField] private BuildingCatalog buildingCatalog;
        [SerializeField] private BuildingMemberCatalog buildingMemberCatalog;

        public PlotController PlotController => plotController;
        public RemodelingController RemodelingController => remodelingController;
        public HouseCatalog HouseCatalog => houseCatalog;
        public BuildingCatalog BuildingCatalog => buildingCatalog;
        public BuildingMemberCatalog BuildingMemberCatalog => buildingMemberCatalog;
        #endregion

        #region Events
        private HanokBuildingSystemEvents events;
        public HanokBuildingSystemEvents Events
        {
            get
            {
                if (events == null)
                {
                    events = new HanokBuildingSystemEvents();
                }
                return events;
            }
        }
        #endregion

        #region State
        [Header("System State")]
        [SerializeField] private SystemState currentState = SystemState.Off;

        public SystemState CurrentState
        {
            get => currentState;
            private set => currentState = value;
        }
        #endregion

        #region Current Data
        [Header("Current Data")]
        [SerializeField] private List<Plot> currentPlots = new List<Plot>();
        [SerializeField] private List<House> currentHouses = new List<House>();
        [SerializeField] private bool isPlotDivisionEnabled = false;

        public List<Plot> CurrentPlots => currentPlots;
        public List<House> CurrentHouses => currentHouses;
        public bool IsPlotDivisionEnabled { get => isPlotDivisionEnabled; set => isPlotDivisionEnabled = value; }

        public Plot GetCurrentPlot()
        {
            return currentPlots != null && currentPlots.Count > 0 ? currentPlots[currentPlots.Count - 1] : null;
        }
        #endregion



        #region Vertex Management
        [Header("Vertex Settings")]
        [SerializeField] private int maxVertexCount = 8;
        private int currentLineIndex = 0;

        public int MaxVertexCount { get => maxVertexCount; set => maxVertexCount = value; }
        #endregion

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            // DontDestroyOnLoad(gameObject);
            Initialize();
        }

        #region Initialization
        private void Initialize()
        {
            if (plotController == null)
            {
                plotController = GetComponentInChildren<PlotController>();
                if (plotController == null)
                {
                    plotController = gameObject.AddComponent<PlotController>();
                }
            }
            
            events = new HanokBuildingSystemEvents();
        }
        #endregion
        
        #region State Management
        public void SetState(SystemState newState)
        {
            SystemState previousState = currentState;
            currentState = newState;
            Events.RaiseStateChanged(previousState, newState);

            switch (currentState)
            {
                case SystemState.Off:
                    OnStateOff();
                    break;
                case SystemState.NewBuilding:
                    OnStateNewBuilding();
                    break;
                case SystemState.Remodeling:
                    OnStateRemodeling();
                    break;
            }
        }

        private void OnStateOff()
        {
            Plot currentPlot = GetCurrentPlot();
            if (currentPlot != null && plotController != null)
            {
                plotController.HidePlot(currentPlot);
            }

            if (currentPlots != null && currentPlots.Count > 0)
            {
                currentPlots.RemoveAt(currentPlots.Count - 1);
            }

            // 라인 인덱스 초기화
            currentLineIndex = 0;

            ClearSelection();
        }

        private void OnStateNewBuilding()
        {
            Plot currentPlot = GetCurrentPlot();
            if (currentPlot == null)
            {
                Plot newPlot = new Plot();
                newPlot.AddLine(new List<Vector3>());
                currentLineIndex = 0;
                currentPlots.Add(newPlot);
                Events.RaisePlotCreated(newPlot);
            }
        }

        private void OnStateRemodeling()
        {
            if (currentHouses.Count == 0)
            {
                Debug.LogWarning("[HanokBuildingSystem] Cannot enter Remodeling mode: No houses selected.");
                SetState(SystemState.Off);
                return;
            }

            // 첫 번째 선택된 House를 리모델링 대상으로 설정
            House targetHouse = currentHouses[0];

            // House의 상태를 UnderConstruction으로 변경 (리모델링 중 표시)
            targetHouse.SetUsageState(HouseOccupancyState.UnderConstruction);

            // 리모델링 시작 이벤트 발생
            Events.RaiseRemodelingStarted(targetHouse);

            Debug.Log($"[HanokBuildingSystem] Entered Remodeling mode for House: {targetHouse.name}");
        }
        #endregion

        #region Public API - House
        public void SelectHouse(House house)
        {
            if (house == null) return;

            if (!currentHouses.Contains(house))
            {
                currentHouses.Add(house);
                Events.RaiseHouseSelected(house);
            }
        }

        public void DeselectHouse(House house)
        {
            if (house != null && currentHouses.Contains(house))
            {
                currentHouses.Remove(house);
                Events.RaiseHouseDeselected(house);
            }
        }

        public void ClearSelection()
        {
            currentHouses.Clear();
            Events.RaiseSelectionCleared();
        }

        public void ReturnCurrentHouses()
        {
            foreach (House house in currentHouses)
            {
                if (house == null) continue;

                List<Building> buildingsToReturn = new List<Building>(house.Buildings);

                foreach (Building building in buildingsToReturn)
                {
                    if (building == null) continue;

                    List<GameObject> membersToReturn = new List<GameObject>(building.BuildingMembers);

                    foreach (GameObject member in membersToReturn)
                    {
                        if (member != null && buildingMemberCatalog != null)
                        {
                            building.RemoveBuildingMember(member);
                            buildingMemberCatalog.ReturnMember(member);
                        }
                    }

                    if (buildingCatalog != null)
                    {
                        house.RemoveBuilding(building);
                        buildingCatalog.ReturnBuilding(building);
                    }
                }

                // 마커의 currentBuilding 초기화
                Transform markersParent = house.transform.Find("Markers");
                if (markersParent != null)
                {
                    foreach (Transform child in markersParent)
                    {
                        MarkerComponent marker = child.GetComponent<MarkerComponent>();
                        if (marker != null)
                        {
                            marker.ClearCurrentBuilding();
                        }
                    }
                }

                if (houseCatalog != null)
                {
                    houseCatalog.ReturnHouse(house);
                }
            }
        }
        #endregion

        #region Public API - Vertex Management
        public void AddVertex(Vector3 worldPosition)
        {
            Plot currentPlot = GetCurrentPlot();
            if (currentPlot == null) return;

            int lineCount = currentPlot.GetLineCount();
            if (lineCount == 0)
            {
                Debug.LogWarning("Has not LineCount of currentPlot.");
                return;
            }
            else if (lineCount >= maxVertexCount)
            {
                // 건설 가능하면 건설 시작
                if (currentPlot.IsBuildable)
                {
                    CheckPlotCompletion();
                }
                return;
            }

            // 첫 클릭
            if(currentPlot.GetLine(0).Count == 0)
            {
                currentPlot.AddVertexToLine(currentLineIndex, worldPosition);  // 시작점
                currentPlot.AddVertexToLine(currentLineIndex, worldPosition);  // 커서
                Events.RaiseVertexAdded(currentPlot, worldPosition);
                return;
            }

            // 마지막 Vertex
            if (lineCount == maxVertexCount - 2)
            {
                // 이전 점 to 지금 점
                currentLineIndex++;
                currentPlot.AddLine(new List<Vector3>());
                currentPlot.AddVertexToLine(currentLineIndex, worldPosition);  // 점3
                currentPlot.AddVertexToLine(currentLineIndex, worldPosition);  // 커서

                // 지금 점 to 시작 점
                Vector3 firstVertex = currentPlot.GetVertex(0, 0);
                currentLineIndex++;
                currentPlot.AddLine(new List<Vector3>());
                currentPlot.AddVertexToLine(currentLineIndex, firstVertex);  // 점3
                currentPlot.AddVertexToLine(currentLineIndex, worldPosition);    // 점1

                Events.RaiseVertexAdded(currentPlot, worldPosition);
                return;
            }

            // 새 라인 생성: [점2, 점2(커서)]
            currentLineIndex++;
            currentPlot.AddLine(new List<Vector3>());
            currentPlot.AddVertexToLine(currentLineIndex, worldPosition);  // 점2
            currentPlot.AddVertexToLine(currentLineIndex, worldPosition);  // 커서

            Events.RaiseVertexAdded(currentPlot, worldPosition);
        }

        public void RemoveLastVertex()
        {
            Plot currentPlot = GetCurrentPlot();
            if (currentPlot == null) return;

            int totalVertices = currentPlot.LineList.Count;

            // 폐곡선 완성 상태 (maxVertexCount 도달)면 마지막 2개 라인 제거
            if (totalVertices >= maxVertexCount)
            {
                // 마지막 라인 제거 (마지막 점 → 첫 점)
                if (currentLineIndex >= 0)
                {
                    currentPlot.RemoveLine(currentLineIndex);
                    currentLineIndex--;
                }

                // 마지막에서 두번째 라인 제거
                if (currentLineIndex >= 0)
                {
                    currentPlot.RemoveLine(currentLineIndex);
                    currentLineIndex = Mathf.Max(0, currentLineIndex - 1);
                }

                Events.RaiseVertexRemoved(currentPlot, currentLineIndex);
                return;
            }

            // 일반 정점 제거
            if (currentLineIndex > 0)
            {
                currentPlot.RemoveLine(currentLineIndex);
                currentLineIndex--;
                Events.RaiseVertexRemoved(currentPlot, currentLineIndex);
            }
            else
            {
                // 첫 번째 라인까지 제거하면 상태 종료
                currentPlot.RemoveLine(0);
                currentLineIndex = 0;
                SetState(SystemState.Off);
            }
        }

        public void UpdateCurrentVertexPosition(Vector3 worldPosition)
        {
            Plot currentPlot = GetCurrentPlot();
            if (currentPlot == null) return;

            var currentLine = currentPlot.GetLine(currentLineIndex);
            if (currentLine != null && currentLine.Count > 0)
            {
                int lastIndex = currentLine.Count - 1;
                currentPlot.UpdateVertex(currentLineIndex, lastIndex, worldPosition);
                if(currentPlot.GetLineCount() > maxVertexCount - 2)
                {
                    currentPlot.UpdateVertex(currentLineIndex-1, lastIndex, worldPosition);
                }
            }
        }

        public void UpdateVisualization()
        {
            if (currentState == SystemState.NewBuilding && currentPlots != null && plotController != null)
            {
                foreach (Plot plot in currentPlots)
                {
                    plotController.ShowPlot(plot);
                }
            }

            if (currentHouses != null && currentPlots != null && houseCatalog != null &&
                currentPlots.Count > 0 && currentPlots[0].GetLineCount() > 2)
            {
                for (int i = 0; i < currentPlots.Count; i++)
                {
                    if (i > currentHouses.Count - 1 || currentHouses[i] == null)
                    {
                        House newHouse = houseCatalog.GetHouse();
                        if (newHouse != null)
                        {
                            currentHouses.Add(newHouse);
                        }
                    }

                    if (i < currentHouses.Count && currentHouses[i] != null && i < currentPlots.Count)
                    {
                        currentHouses[i].ShowModelHouse(currentPlots[i]);
                    }
                }
            }
        }

        private void CheckPlotCompletion()
        {
            Plot currentPlot = GetCurrentPlot();
            if (currentPlot != null && currentPlot.IsBuildable)
            {
                Events.RaisePlotCompleted(currentPlot);
                ValidatePlotForConstruction();
            }
        }
        #endregion

        #region Plot Validation
        private void ValidatePlotForConstruction()
        {
            if (currentHouses.Count == 0)
            {
                Debug.LogWarning("No houses to build.");
                return;
            }

            bool canDivide = CheckPlotDivision();

            if (canDivide && isPlotDivisionEnabled)
            {
                DividePlot();
            }

            bool allHousesValid = ValidateAllHouses();

            if (allHousesValid)
            {
                StartConstruction();
            }
        }

        private bool CheckPlotDivision()
        {
            Plot currentPlot = GetCurrentPlot();
            if (currentHouses.Count == 0 || currentPlot == null) return false;

            float plotArea = currentPlot.GetArea();
            float minHouseArea = float.MaxValue;

            foreach (var house in currentHouses)
            {
                if (house == null) continue;

                float houseArea = house.SampleSize.x * house.SampleSize.y;
                if (houseArea < minHouseArea)
                {
                    minHouseArea = houseArea;
                }
            }

            if (minHouseArea == float.MaxValue) return false;

            return plotArea >= minHouseArea * 2f;
        }

        private void DividePlot()
        {
            Plot currentPlot = GetCurrentPlot();
            if (plotController != null && currentPlot != null)
            {
                List<Plot> dividedPlots = plotController.DividePlot_Horizontal(currentPlot, 2);

                if (dividedPlots != null && dividedPlots.Count > 0)
                {
                    Debug.Log($"Plot divided into {dividedPlots.Count} sub-plots.");
                }
            }
        }

        private bool ValidateAllHouses()
        {
            Plot currentPlot = GetCurrentPlot();
            if (currentPlot == null || currentPlot.GetLineCount() < 3)
            {
                Debug.LogWarning("Plot is invalid for house validation.");
                return false;
            }

            Vector3 min = currentPlot.AllVertices[0];
            Vector3 max = currentPlot.AllVertices[0];

            foreach (var vertex in currentPlot.AllVertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            float plotWidth = max.x - min.x;
            float plotDepth = max.z - min.z;

            foreach (var house in currentHouses)
            {
                if (house == null) continue;

                if (house.SampleSize.x > plotWidth || house.SampleSize.y > plotDepth)
                {
                    Debug.LogWarning($"House {house.name} does not fit within plot bounds. " +
                                   $"House size: {house.SampleSize}, Plot size: ({plotWidth}, {plotDepth})");
                    return false;
                }
            }

            return true;
        }

        public void StartConstruction()
        {
            Plot currentPlot = GetCurrentPlot();
            if (currentPlot == null || buildingCatalog == null)
            {
                Debug.LogWarning("Cannot start construction: missing plot or building catalog.");
                return;
            }

            Events.RaiseConstructionStarted();

            Vector3 plotCenter = currentPlot.GetCenter();
            float spacing = 5f;

            for (int i = 0; i < currentHouses.Count; i++)
            {
                House house = currentHouses[i];
                if (house == null) continue;

                Vector3 housePosition = plotCenter + new Vector3(i * spacing, 0, 0);
                house.transform.position = housePosition;

                foreach (BuildingType requiredType in house.RequiredBuildingTypes)
                {
                    if (requiredType == BuildingType.None) continue;

                    Building building = buildingCatalog.GetBuildingByType(
                        requiredType,
                        housePosition,
                        Quaternion.identity
                    );

                    if (building != null)
                    {
                        house.AddBuilding(building);
                        Events.RaiseBuildingConstructed(building);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to get building of type {requiredType} from catalog.");
                    }
                }

                house.SetUsageState(HouseOccupancyState.UnderConstruction);
            }

            SetState(SystemState.Off);
        }

        public void CancelConstruction()
        {
            if (currentHouses == null || currentHouses.Count == 0)
            {
                SetState(SystemState.Off);
                return;
            }

            ReturnCurrentHouses();

            ClearSelection();
            Events.RaiseConstructionCancelled();
            SetState(SystemState.Off);
        }
        #endregion
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
