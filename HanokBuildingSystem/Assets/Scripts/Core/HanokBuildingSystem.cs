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
        [SerializeField] private WallGenerator wallGenerator;

        [Header("Catalogs")]
        [SerializeField] private HouseCatalog houseCatalog;
        [SerializeField] private BuildingCatalog buildingCatalog;
        [SerializeField] private BuildingMemberCatalog buildingMemberCatalog;

        [Header("Terrain Settings")]
        [Tooltip("건축물 배치 시 지형 높이를 따를지 여부")]
        [SerializeField] private bool useTerrainHeight = true;
        [Tooltip("지형 감지용 LayerMask")]
        [SerializeField] private LayerMask groundLayerMask = -1;
        [Tooltip("허용 가능한 최대 기울기 (0~90도)")]
        [Range(0f, 90f)]
        [SerializeField] private float maxAllowedSlope = 30f;

        public PlotController PlotController => plotController;
        public RemodelingController RemodelingController => remodelingController;
        public WallGenerator WallGenerator => wallGenerator;
        public HouseCatalog HouseCatalog => houseCatalog;
        public BuildingCatalog BuildingCatalog => buildingCatalog;
        public BuildingMemberCatalog BuildingMemberCatalog => buildingMemberCatalog;
        public bool UseTerrainHeight => useTerrainHeight;
        public LayerMask GroundLayerMask => groundLayerMask;
        public float MaxAllowedSlope { get => maxAllowedSlope; set => maxAllowedSlope = value; }
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

        [Header("Intermediate Points")]
        [Tooltip("라인 사이에 중간 정점을 생성할지 여부")]
        [SerializeField] private bool enableIntermediatePoints = true;
        [Tooltip("중간 정점 간격 (미터)")]
        [SerializeField] private float intermediatePointSpacing = 1.0f;

        private int currentLineIndex = 0;

        public int MaxVertexCount { get => maxVertexCount; set => maxVertexCount = value; }
        public bool EnableIntermediatePoints { get => enableIntermediatePoints; set => enableIntermediatePoints = value; }
        public float IntermediatePointSpacing { get => intermediatePointSpacing; set => intermediatePointSpacing = Mathf.Max(0.1f, value); }
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

                // PlotController에 허용 기울기 전달
                if (plotController != null)
                {
                    plotController.MaxAllowedSlope = maxAllowedSlope;
                }
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

            // RemodelingController에 리모델링 시작 알림 (백업 수행)
            if (remodelingController != null)
            {
                remodelingController.StartRemodeling(targetHouse);
                
                Events.RaiseRemodelingStarted(targetHouse);
            }

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
            Events.RaiseSelectionClearing(currentHouses);
            currentHouses.Clear();
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
                currentPlot.AddVertexToLine(currentLineIndex, worldPosition);  // 점3
                currentPlot.AddVertexToLine(currentLineIndex, firstVertex);    // 점1

                Events.RaiseVertexAdded(currentPlot, worldPosition);
                return;
            }
            else
            {
                // Check plot completion
                lineCount = currentPlot.GetLineCount();
                if (lineCount == 0)
                {
                    Debug.LogWarning("Has not LineCount of currentPlot.");
                    return;
                }
                else if (lineCount >= maxVertexCount)
                {
                    CheckPlotCompletion();
                    return;
                }
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
            if (currentLine == null || currentLine.Count == 0) return;

            // useTerrainHeight가 false면 모든 점을 평평하게 유지
            if (!useTerrainHeight)
            {
                // 첫 라인의 첫 점 높이를 기준으로 사용
                float baseHeight = currentPlot.GetVertex(0, 0).y;
                worldPosition.y = baseHeight;

                // 기존 모든 점들도 같은 높이로 조정
                NormalizeAllPointsHeight(currentPlot, baseHeight);
            }

            // 중간점 생성이 활성화되어 있으면 라인 전체를 재생성
            if (enableIntermediatePoints)
            {
                // 마지막 라인이 생성된 경우
                if (currentPlot.GetLineCount() > maxVertexCount - 2)
                {
                    // 마지막 라인: [worldPosition(커서), firstVertex]
                    Vector3 firstVertex = currentPlot.GetVertex(0, 0);
                    List<Vector3> newLastLine = GenerateIntermediatePoints(worldPosition, firstVertex);
                    currentPlot.UpdateVertices(currentLineIndex, newLastLine);

                    // 마지막에서 두 번째 라인: [이전점, worldPosition(커서)]
                    var prevLine = currentPlot.GetLine(currentLineIndex - 1);
                    if (prevLine != null && prevLine.Count > 0)
                    {
                        Vector3 prevStartPoint = prevLine[0];
                        List<Vector3> newPrevLine = GenerateIntermediatePoints(prevStartPoint, worldPosition);
                        currentPlot.UpdateVertices(currentLineIndex - 1, newPrevLine);
                    }
                }
                else
                {
                    // 일반 라인: [첫점, ..., 커서]
                    Vector3 startPoint = currentLine[0];
                    List<Vector3> newLine = GenerateIntermediatePoints(startPoint, worldPosition);
                    currentPlot.UpdateVertices(currentLineIndex, newLine);
                }
            }
            else
            {
                // 중간점 생성이 비활성화되어 있으면 기존 방식 사용
                int lastIndex = currentLine.Count - 1;

                if (currentPlot.GetLineCount() > maxVertexCount - 2)
                {
                    currentPlot.UpdateVertex(currentLineIndex, 0, worldPosition);
                    currentPlot.UpdateVertex(currentLineIndex - 1, lastIndex, worldPosition);
                }
                else
                {
                    currentPlot.UpdateVertex(currentLineIndex, lastIndex, worldPosition);
                }
            }
        }

        /// <summary>
        /// 시작점부터 끝점까지 등간격으로 중간점들을 생성하며, 각 점의 높이는 지형에 맞춤
        /// </summary>
        private List<Vector3> GenerateIntermediatePoints(Vector3 startPoint, Vector3 endPoint)
        {
            List<Vector3> points = new List<Vector3>();

            // XZ 평면에서의 거리 계산
            Vector3 startXZ = new Vector3(startPoint.x, 0, startPoint.z);
            Vector3 endXZ = new Vector3(endPoint.x, 0, endPoint.z);
            float horizontalDistance = Vector3.Distance(startXZ, endXZ);

            // 간격이 너무 짧거나 거리가 짧으면 시작점과 끝점만 반환
            if (horizontalDistance <= intermediatePointSpacing || intermediatePointSpacing <= 0.01f)
            {
                points.Add(startPoint);
                points.Add(endPoint);
                return points;
            }

            // 필요한 점 개수 계산
            int segmentCount = Mathf.Max(1, Mathf.CeilToInt(horizontalDistance / intermediatePointSpacing));           

            // 지형 높이 고려
            if (useTerrainHeight)
            {
                points.Add(startPoint);

                for (int i = 1; i < segmentCount; i++)
                {
                    float t = (float)i / segmentCount;
                    Vector3 xzPosition = Vector3.Lerp(startXZ, endXZ, t);

                    // 지형 높이 감지 (위에서 아래로 Raycast)
                    float terrainHeight = GetTerrainHeightAtPosition(xzPosition);

                    Vector3 intermediatePoint = new Vector3(xzPosition.x, terrainHeight, xzPosition.z);
                    points.Add(intermediatePoint);
                }

                points.Add(endPoint);
            }
            else
            {
                points.Add(new Vector3(startPoint.x, currentPlots[0].GetLine(0)[0].y , startPoint.z));
                points.Add(new Vector3(endPoint.x, currentPlots[0].GetLine(0)[0].y , endPoint.z));
            }

            return points;
        }

        /// <summary>
        /// 특정 XZ 위치에서의 지형 높이를 Raycast로 감지
        /// CPU 효율을 위해 단일 Raycast만 사용
        /// </summary>
        private float GetTerrainHeightAtPosition(Vector3 xzPosition)
        {
            // 충분히 높은 곳에서 아래로 Raycast
            Vector3 rayOrigin = new Vector3(xzPosition.x, 1000f, xzPosition.z);
            Ray ray = new Ray(rayOrigin, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, 2000f, groundLayerMask))
            {
                return hit.point.y;
            }

            // 실패 시 기본 높이 (0 또는 xzPosition.y)
            return 0f;
        }

        /// <summary>
        /// Plot의 모든 점들의 높이를 기준 높이로 정규화 (평평하게 만들기)
        /// </summary>
        private void NormalizeAllPointsHeight(Plot plot, float baseHeight)
        {
            if (plot == null || plot.LineList == null) return;

            for (int lineIdx = 0; lineIdx < plot.LineList.Count; lineIdx++)
            {
                var line = plot.GetLine(lineIdx);
                if (line == null) continue;

                for (int vertexIdx = 0; vertexIdx < line.Count; vertexIdx++)
                {
                    Vector3 vertex = line[vertexIdx];
                    vertex.y = baseHeight;
                    plot.UpdateVertex(lineIdx, vertexIdx, vertex);
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
                currentPlots.Count > 0 && currentPlots[0].GetLineCount() > 3)
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
                        currentHouses[i].ShowModelHouse(currentPlots[i], useTerrainHeight, groundLayerMask);
                    }
                }
            }
        }

        private void CheckPlotCompletion()
        {
            Plot currentPlot = GetCurrentPlot();
            if (currentPlot != null && plotController != null && plotController.IsBuildable(currentPlot))
            {
                ValidatePlotForConstruction();
                Events.RaisePlotCompleted(currentPlot);
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

        /// <summary>
        /// 최소크기 검사
        /// </summary>
        /// <returns></returns>
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
            Debug.Log("StartConstruction");
            Plot currentPlot = GetCurrentPlot();
            if (currentPlot == null || buildingCatalog == null)
            {
                Debug.LogWarning("Cannot start construction: missing plot or building catalog.");
                return;
            }


            for (int i = 0; i < currentHouses.Count; i++)
            {
                House house = currentHouses[i];
                if (house == null) continue;

                house.StartConstruction(currentPlot);

                Events.RaiseConstructionStarted(house);

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

            Events.RaiseConstructionCancelled(currentHouses[0]);

            ReturnCurrentHouses();

            ClearSelection();
            
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
