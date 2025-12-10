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

        private Dictionary<Plot, GameObject> plotVisuals = new Dictionary<Plot, GameObject>();

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

                // 3개 이상의 라인이 있으면 면(Fill Mesh)도 생성
                if (line.Count >= 3)
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

                    Plot newPlot = new Plot(
                        new List<List<Vector3>> { newLineVertices },
                        plot.LineThickness,
                        plot.MinAngle,
                        plot.MaxAngle
                    );

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
