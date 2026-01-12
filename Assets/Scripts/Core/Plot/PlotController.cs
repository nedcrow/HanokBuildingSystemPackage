using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// 선 스타일 타입
    /// </summary>
    public enum LineStyle
    {
        Solid,      // 실선
        Dashed      // 점선
    }

    public class PlotController : MonoBehaviour
    {
        [Header("Plot Visualization")]
        [SerializeField] private Material plotMeshMaterial;
        [SerializeField] private Material plotLineMaterial;
        [SerializeField] private float lineWidth = 0.1f;

        [Header("Line Style Settings")]
        [SerializeField] private LineStyle lineStyle = LineStyle.Solid;
        [Tooltip("점선 간격 (LineStyle이 Dashed일 때만 적용)")]
        [SerializeField] private float dashLength = 0.5f;
        [Tooltip("점선 사이 공백 (LineStyle이 Dashed일 때만 적용)")]
        [SerializeField] private float gapLength = 0.5f;

        [Header("Plot Settings")]
        [Tooltip("Plot 라인 두께")]
        [SerializeField] private float defaultLineThickness = 0.1f;
        [Tooltip("허용 최소 각도")]
        [Range(0f, 179f)]
        [SerializeField] private float minAngle = 45f;
        [Tooltip("허용 최대 각도")]
        [Range(0f, 179f)]
        [SerializeField] private float maxAngle = 135f;

        [Header("Terrain Settings")]
        [Tooltip("허용 가능한 최대 기울기 (0~90도)")]
        [Range(0f, 90f)]
        [SerializeField] private float maxAllowedSlope = 30f;

        private Dictionary<Plot, GameObject> plotVisuals = new Dictionary<Plot, GameObject>();

        public float LineThickness { get => defaultLineThickness; set => defaultLineThickness = value; }
        public float MinAngle { get => minAngle; set => minAngle = value; }
        public float MaxAngle { get => maxAngle; set => maxAngle = value; }
        public float MaxAllowedSlope { get => maxAllowedSlope; set => maxAllowedSlope = value; }

        public void ShowPlot(Plot plot)
        {
            if (plot == null || plot.LineList == null || plot.LineList.Count == 0) return;

            if (plotVisuals.ContainsKey(plot))
            {
                Destroy(plotVisuals[plot]);
                plotVisuals.Remove(plot);
            }

            GameObject plotVisual = new GameObject("PlotVisual");
            plotVisual.transform.SetParent(transform);

            // VerticesList의 각 라인마다 LineRenderer와 Mesh 생성
            for (int i = 0; i < plot.LineList.Count; i++)
            {
                List<Vector3> line = plot.LineList[i];
                if (line.Count < 2) continue;

                // 각 라인마다 별도의 LineRenderer 생성
                CreateLineMesh(plotVisual, line, i);

                if(plot.LineList.Count > 1 )
                {
                    IsBuildable(plot);
                }
                else if (plot.LineList.Count > 2)
                {
                    CreateFillMesh(plotVisual, line, i);
                }
            }

            plotVisuals[plot] = plotVisual;
        }

        public void HidePlot(Plot plot)
        {
            if (plotVisuals.ContainsKey(plot))
            {
                Destroy(plotVisuals[plot]);
                plotVisuals.Remove(plot);
            }
        }

        public void HideAllPlot()
        {
            foreach (var kvp in plotVisuals)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }

            plotVisuals.Clear();
        }

        public List<Plot> DividePlot_Horizontal(Plot plot, int count)
        {
            if (count <= 1)
            {
                Debug.LogWarning("Count must be greater than 1 for division.");
                return new List<Plot> { plot };
            }

            return DividePlot(plot, count, true);
        }

        public List<Plot> DividePlot_Vertical(Plot plot, int count)
        {
            if (count <= 1)
            {
                Debug.LogWarning("Count must be greater than 1 for division.");
                return new List<Plot> { plot };
            }

            return DividePlot(plot, count, false);
        }

        private List<Plot> DividePlot(Plot plot, int count, bool isHorizontal)
        {
            List<Plot> dividedPlots = new List<Plot>();

            if (plot.LineList.Count == 0)
            {
                return dividedPlots;
            }

            foreach (var line in plot.LineList)
            {
                if (line.Count < 4) continue;

                Bounds bounds = CalculateBounds(line);
                float divisionSize = isHorizontal ? bounds.size.x / count : bounds.size.z / count;

                for (int i = 0; i < count; i++)
                {
                    List<Vector3> newLineVertices = new List<Vector3>();

                    if (isHorizontal)
                    {
                        float startX = bounds.min.x + (divisionSize * i);
                        float endX = bounds.min.x + (divisionSize * (i + 1));

                        newLineVertices.Add(new Vector3(startX, bounds.min.y, bounds.min.z));
                        newLineVertices.Add(new Vector3(endX, bounds.min.y, bounds.min.z));
                        newLineVertices.Add(new Vector3(endX, bounds.max.y, bounds.max.z));
                        newLineVertices.Add(new Vector3(startX, bounds.max.y, bounds.max.z));
                    }
                    else
                    {
                        float startZ = bounds.min.z + (divisionSize * i);
                        float endZ = bounds.min.z + (divisionSize * (i + 1));

                        newLineVertices.Add(new Vector3(bounds.min.x, bounds.min.y, startZ));
                        newLineVertices.Add(new Vector3(bounds.max.x, bounds.min.y, startZ));
                        newLineVertices.Add(new Vector3(bounds.max.x, bounds.max.y, endZ));
                        newLineVertices.Add(new Vector3(bounds.min.x, bounds.max.y, endZ));
                    }

                    Plot newPlot = new Plot(new List<List<Vector3>> { newLineVertices });

                    dividedPlots.Add(newPlot);
                }
            }

            return dividedPlots;
        }

        private Bounds CalculateBounds(List<Vector3> vertices)
        {
            if (vertices.Count == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Vector3 min = vertices[0];
            Vector3 max = vertices[0];

            foreach (var vertex in vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;

            return new Bounds(center, size);
        }

        /// <summary>
        /// 하나의 라인에 대한 LineRenderer 생성
        /// </summary>
        /// <param name="parent">부모 GameObject</param>
        /// <param name="vertices">라인의 정점들</param>
        /// <param name="lineIndex">라인 인덱스 (이름 구분용)</param>
        private void CreateLineMesh(GameObject parent, List<Vector3> vertices, int lineIndex)
        {
            GameObject lineObject = new GameObject($"PlotLine_{lineIndex}");
            lineObject.transform.SetParent(parent.transform);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = vertices.Count;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.loop = false;

            if (plotLineMaterial != null)
            {
                lineRenderer.material = plotLineMaterial;
            }

            // 선 스타일 적용
            ApplyLineStyle(lineRenderer);

            // 정점 위치 설정
            for (int i = 0; i < vertices.Count; i++)
            {
                lineRenderer.SetPosition(i, vertices[i]);
            }
        }

        /// <summary>
        /// LineRenderer에 선 스타일(실선/점선) 적용
        /// </summary>
        private void ApplyLineStyle(LineRenderer lineRenderer)
        {
            if (lineRenderer == null) return;

            switch (lineStyle)
            {
                case LineStyle.Solid:
                    // 실선: TextureMode를 Stretch로 설정
                    lineRenderer.textureMode = LineTextureMode.Stretch;
                    break;

                case LineStyle.Dashed:
                    // 점선: TextureMode를 Tile로 설정하고 Material의 Tiling 조정
                    lineRenderer.textureMode = LineTextureMode.Tile;

                    // Material이 있으면 Tiling 값 설정
                    if (lineRenderer.material != null)
                    {
                        // Dash pattern을 위한 텍스처 스케일 계산
                        float totalLength = dashLength + gapLength;
                        float tilingScale = 1f / totalLength;

                        lineRenderer.material.mainTextureScale = new Vector2(tilingScale, 1f);

                        // Material이 Shader에서 _MainTex_ST를 사용하는 경우
                        // Tiling과 Offset 설정
                        lineRenderer.material.SetTextureScale("_MainTex", new Vector2(tilingScale, 1f));
                    }
                    break;
            }
        }

        /// <summary>
        /// 선 스타일을 실시간으로 변경
        /// </summary>
        public void SetLineStyle(LineStyle style)
        {
            lineStyle = style;

            // 현재 표시 중인 모든 Plot의 LineRenderer 업데이트
            foreach (var visual in plotVisuals.Values)
            {
                if (visual != null)
                {
                    LineRenderer[] lineRenderers = visual.GetComponentsInChildren<LineRenderer>();
                    foreach (var lineRenderer in lineRenderers)
                    {
                        ApplyLineStyle(lineRenderer);
                    }
                }
            }
        }

        /// <summary>
        /// 점선 간격 설정
        /// </summary>
        public void SetDashSettings(float dash, float gap)
        {
            dashLength = Mathf.Max(0.1f, dash);
            gapLength = Mathf.Max(0.1f, gap);

            // LineStyle이 Dashed일 때만 업데이트
            if (lineStyle == LineStyle.Dashed)
            {
                SetLineStyle(LineStyle.Dashed);
            }
        }

        /// <summary>
        /// 하나의 라인에 대한 Fill Mesh 생성
        /// </summary>
        /// <param name="parent">부모 GameObject</param>
        /// <param name="vertices">라인의 정점들</param>
        /// <param name="meshIndex">메시 인덱스 (이름 구분용)</param>
        private void CreateFillMesh(GameObject parent, List<Vector3> vertices, int meshIndex)
        {
            GameObject meshObject = new GameObject($"PlotMesh_{meshIndex}");
            meshObject.transform.SetParent(parent.transform);

            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

            if (plotMeshMaterial != null)
            {
                meshRenderer.material = plotMeshMaterial;
            }

            Mesh mesh = new Mesh();
            Vector3[] meshVertices = new Vector3[vertices.Count];
            int[] triangles = new int[(vertices.Count - 2) * 3];

            for (int i = 0; i < vertices.Count; i++)
            {
                meshVertices[i] = vertices[i];
            }

            // Fan triangulation (첫 정점을 중심으로 삼각형 생성)
            for (int i = 0; i < vertices.Count - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = meshVertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }

        /// <summary>
        /// Plot의 최대 기울기 계산 (0~90도)
        /// 가장 낮은 정점과 가장 높은 정점 사이의 기울기를 반환
        /// </summary>
        public float CalculateMaxSlope(Plot plot)
        {
            if (plot == null || plot.AllVertices == null || plot.AllVertices.Count < 2)
                return 0f;

            // 가장 낮은 정점과 가장 높은 정점 찾기
            Vector3 lowestVertex = plot.AllVertices[0];
            Vector3 highestVertex = plot.AllVertices[0];

            foreach (var vertex in plot.AllVertices)
            {
                if (vertex.y < lowestVertex.y)
                    lowestVertex = vertex;
                if (vertex.y > highestVertex.y)
                    highestVertex = vertex;
            }

            // 높이 차이
            float heightDiff = highestVertex.y - lowestVertex.y;

            // 높이가 같으면 기울기 0
            if (Mathf.Approximately(heightDiff, 0f))
                return 0f;

            // 수평 거리 (XZ 평면)
            float horizontalDistance = new Vector2(
                highestVertex.x - lowestVertex.x,
                highestVertex.z - lowestVertex.z
            ).magnitude;

            // 수평 거리가 0이면 90도 (수직)
            if (Mathf.Approximately(horizontalDistance, 0f))
                return 90f;

            // 기울기 각도 = atan(높이차 / 수평거리) * (180 / π)
            float slopeAngle = Mathf.Atan(heightDiff / horizontalDistance) * Mathf.Rad2Deg;

            return slopeAngle;
        }

        /// <summary>
        /// Plot의 기울기가 허용 범위 내인지 확인
        /// </summary>
        public bool IsSlopeValid(Plot plot)
        {
            float currentSlope = CalculateMaxSlope(plot);
            bool isValid = currentSlope <= maxAllowedSlope;

            // 디버그 로그 (기울기 초과 시)
            if (!isValid)
            {
                Debug.LogWarning($"[PlotController] 기울기 초과: {currentSlope:F1}° > {maxAllowedSlope:F1}°");
            }

            return isValid;
        }

        /// <summary>
        /// Plot의 각도 검증
        /// 1. Line 내부: 최소각(A)만 검사 (minAngle 이상)
        /// 2. Line 간 접점: minAngle ~ maxAngle 범위 검사
        /// </summary>
        public bool IsValidateAngles(Plot plot)
        {
            if (plot == null || plot.LineList == null) return false;
            if (plot.LineList.Count == 0) return true;

            // 1. 각 Line 내부의 최소각 검사
            for (int lineIdx = 0; lineIdx < plot.LineList.Count; lineIdx++)
            {
                var line = plot.LineList[lineIdx];

                if (line == null || line.Count < 2) continue;

                // Line 내부 정점들 (시작점과 끝점 제외)
                float minAngleInLine = float.MaxValue;

                for (int i = 1; i < line.Count - 1; i++)
                {
                    Vector3 v1 = line[i - 1] - line[i];
                    Vector3 v2 = line[i + 1] - line[i];
                    float angle = Vector3.Angle(v1, v2);

                    if (angle < minAngleInLine)
                        minAngleInLine = angle;
                }

                // 내부 정점이 있는 경우 최소각 검사
                if (minAngleInLine != float.MaxValue)
                {
                    if (minAngleInLine < minAngle)
                    {
                        Debug.LogWarning($"[PlotController] Line {lineIdx} 내부 최소각 검증 실패: {minAngleInLine:F1}° < {minAngle:F1}°");
                        return false;
                    }
                }
            }

            // 2. Line 간 접점에서의 각도 검사
            for (int lineIdx = 0; lineIdx < plot.LineList.Count; lineIdx++)
            {
                var currentLine = plot.LineList[lineIdx];
                var nextLine = plot.LineList[(lineIdx + 1) % plot.LineList.Count];

                if (currentLine == null || currentLine.Count < 2) continue;
                if (nextLine == null || nextLine.Count < 2) continue;

                // 접점은 currentLine의 마지막 점이자 nextLine의 첫 점
                Vector3 connectionPoint = currentLine[currentLine.Count - 1];

                // 접점 이전 점 (currentLine의 마지막에서 두 번째)
                Vector3 prevPoint = currentLine[currentLine.Count - 2];

                // 접점 이후 점 (nextLine의 두 번째 점)
                Vector3 nextPoint = nextLine.Count > 1 ? nextLine[1] : nextLine[0];

                Vector3 v1 = prevPoint - connectionPoint;
                Vector3 v2 = nextPoint - connectionPoint;

                float connectionAngle = Vector3.Angle(v1, v2);

                bool isValid = connectionAngle >= minAngle && connectionAngle <= maxAngle;
                if (!isValid) return false;
            }

            return true;
        }


        /// <summary>
        /// Plot의 건축 가능 여부 판정
        /// </summary>
        public bool IsBuildable(Plot plot)
        {
            if (plot == null) return false;

            bool hasEnoughVertices = plot.AllVertices != null && plot.AllVertices.Count >= 4;
            bool anglesValid = IsValidateAngles(plot);
            bool slopeValid = IsSlopeValid(plot);

            return hasEnoughVertices && anglesValid && slopeValid;
        }

        private void OnDestroy()
        {
            foreach (var visual in plotVisuals.Values)
            {
                if (visual != null)
                {
                    Destroy(visual);
                }
            }
            plotVisuals.Clear();
        }
    }
}
