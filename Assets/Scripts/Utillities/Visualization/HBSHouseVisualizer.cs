using UnityEngine;
using System.Collections.Generic;

namespace HanokBuildingSystem
{
    /// <summary>
    /// House의 아웃라인 영역을 시각화하는 컴포넌트
    /// House가 선택되었을 때 범위를 LineRenderer로 표시
    /// </summary>
    public class HBSHouseVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private Material rangeMaterial;
        [SerializeField] private Color rangeColor = new Color(1f, 1f, 0f, 0.8f);
        [SerializeField] private float lineWidth = 0.2f;
        [SerializeField] private float lineHeightOffset = 0.1f;

        [Header("LineRenderer Settings")]
        [SerializeField] private int cornerVertexCount = 1;
        [SerializeField] private int endCapVertexCount = 1;
        [SerializeField] private LineTextureMode textureMode = LineTextureMode.Tile;

        private House house;
        private List<LineRenderer> lineRenderers = new List<LineRenderer>();
        private bool isVisible = false;

        void Awake()
        {
            house = GetComponent<House>();

            if (house == null)
            {
                Debug.LogError("HBSHouseVisualizer: House 컴포넌트를 찾을 수 없습니다.");
                enabled = false;
            }
        }

        /// <summary>
        /// House의 아웃라인 범위를 표시
        /// </summary>
        public void ShowRange()
        {
            if (house == null || house.OutlineVertices == null) return;

            DrawRange(house.OutlineVertices);
            isVisible = true;
        }

        /// <summary>
        /// 아웃라인 범위를 숨김
        /// </summary>
        public void HideRange()
        {
            foreach (var lineRenderer in lineRenderers)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                }
            }
            isVisible = false;
        }

        /// <summary>
        /// 범위 색상 변경
        /// </summary>
        public void SetColor(Color color)
        {
            rangeColor = color;

            foreach (var lineRenderer in lineRenderers)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.startColor = color;
                    lineRenderer.endColor = color;
                }
            }
        }

        /// <summary>
        /// 라인 두께 변경
        /// </summary>
        public void SetLineWidth(float width)
        {
            lineWidth = width;

            foreach (var lineRenderer in lineRenderers)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.startWidth = width;
                    lineRenderer.endWidth = width;
                }
            }
        }

        /// <summary>
        /// 2차원 아웃라인 리스트를 받아 LineRenderer로 그림
        /// </summary>
        private void DrawRange(List<List<Vector3>> outlineLoops)
        {
            if (outlineLoops == null || outlineLoops.Count == 0) return;

            // 필요한 LineRenderer 개수만큼 확보
            EnsureLineRendererCount(outlineLoops.Count);

            // 각 아웃라인 루프를 그림
            for (int i = 0; i < outlineLoops.Count; i++)
            {
                DrawSingleLoop(lineRenderers[i], outlineLoops[i]);
            }

            // 남은 LineRenderer는 비활성화
            for (int i = outlineLoops.Count; i < lineRenderers.Count; i++)
            {
                lineRenderers[i].enabled = false;
            }
        }

        /// <summary>
        /// 하나의 아웃라인 루프를 LineRenderer로 그림
        /// </summary>
        private void DrawSingleLoop(LineRenderer lineRenderer, List<Vector3> vertices)
        {
            if (lineRenderer == null || vertices == null || vertices.Count < 2) return;

            // LineRenderer 설정
            lineRenderer.enabled = true;
            lineRenderer.loop = true;
            lineRenderer.positionCount = vertices.Count;

            // 정점 위치 설정 (높이 오프셋 적용)
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 position = vertices[i];
                position.y += lineHeightOffset;
                lineRenderer.SetPosition(i, position);
            }
        }

        /// <summary>
        /// 필요한 LineRenderer 개수만큼 생성 또는 재사용
        /// </summary>
        private void EnsureLineRendererCount(int count)
        {
            // 부족한 경우 추가 생성
            while (lineRenderers.Count < count)
            {
                GameObject lineObj = new GameObject($"OutlineLine_{lineRenderers.Count}");
                lineObj.transform.SetParent(transform);
                lineObj.transform.localPosition = Vector3.zero;
                lineObj.transform.localRotation = Quaternion.identity;

                LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
                ConfigureLineRenderer(lineRenderer);

                lineRenderers.Add(lineRenderer);
            }
        }

        /// <summary>
        /// LineRenderer의 기본 설정 구성
        /// </summary>
        private void ConfigureLineRenderer(LineRenderer lineRenderer)
        {
            if (lineRenderer == null) return;

            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.startColor = rangeColor;
            lineRenderer.endColor = rangeColor;
            lineRenderer.material = rangeMaterial;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.numCornerVertices = cornerVertexCount;
            lineRenderer.numCapVertices = endCapVertexCount;
            lineRenderer.textureMode = textureMode;
            lineRenderer.enabled = false;
        }

        /// <summary>
        /// 정리 작업
        /// </summary>
        void OnDestroy()
        {
            foreach (var lineRenderer in lineRenderers)
            {
                if (lineRenderer != null)
                {
                    Destroy(lineRenderer.gameObject);
                }
            }
            lineRenderers.Clear();
        }

        /// <summary>
        /// 현재 범위가 표시 중인지 여부
        /// </summary>
        public bool IsVisible => isVisible;
    }
}
  