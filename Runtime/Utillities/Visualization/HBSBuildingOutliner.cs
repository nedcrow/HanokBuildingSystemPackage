using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HanokBuildingSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Building))]
    public sealed class HBSBuildingOutliner : MonoBehaviour, IPlacementFeedback
    {
        [Header("Outline")]
        [SerializeField] private Material outlineMaterial;

        [Header("Style")]
        [SerializeField] private float widthSelected = 0.2f;
        [SerializeField] private float widthValid = 0.1f;
        [SerializeField] private float widthInvalid = 0.1f;

        [SerializeField] private Color colorSelected = Color.cyan;
        [SerializeField] private Color colorValid = Color.green;
        [SerializeField] private Color colorInvalid = Color.red;

        private readonly List<MeshRenderer> _outlineRenderers = new();

        private MaterialPropertyBlock _mpb;

        private PlacementVisualState _current = PlacementVisualState.None;

        private sealed class OutlineMarker : MonoBehaviour { }

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            if (outlineMaterial == null)
            {
                Debug.LogWarning($"[{name}] outlineMaterial is null. Outliner disabled.");
                return;
            }

            RebuildOutline();
            ClearPlacementState();
        }

        public void RebuildOutline()
        {
            for (int i = _outlineRenderers.Count - 1; i >= 0; i--)
            {
                var r = _outlineRenderers[i];
                if (r != null) Destroy(r.gameObject);
            }
            _outlineRenderers.Clear();

            if (outlineMaterial == null) return;

            var sources = GetComponentsInChildren<MeshRenderer>(true);

            foreach (var src in sources)
            {
                if (src == null) continue;
                if (src.GetComponent<OutlineMarker>() != null) continue;

                var srcMf = src.GetComponent<MeshFilter>();
                if (srcMf == null || srcMf.sharedMesh == null) continue;

                var go = new GameObject($"{src.gameObject.name}_Outline");
                go.transform.SetParent(src.transform, false);
                go.AddComponent<OutlineMarker>();

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = srcMf.sharedMesh;

                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = outlineMaterial;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                _outlineRenderers.Add(mr);
            }
        }

        public void SetPlacementState(PlacementVisualState state)
        {
            if (state == _current) return;
            _current = state;

            switch (state)
            {
                case PlacementVisualState.Selected:
                    Apply(colorSelected, widthSelected, true);
                    break;
                case PlacementVisualState.Valid:
                    Apply(colorValid, widthValid, true);
                    break;
                case PlacementVisualState.Invalid:
                    Apply(colorInvalid, widthInvalid, true);
                    break;
                default:
                    ClearPlacementState();
                    break;
            }
        }

        public void ClearPlacementState()
        {
            _current = PlacementVisualState.None;
            Apply(Color.clear, 0f, false);
        }

        private void Apply(Color color, float width, bool active)
        {
            // (안전장치) 혹시 Awake 전에 호출될 가능성 있으면 방어
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            _mpb.Clear();
            _mpb.SetColor("_OutlineColor", color);
            _mpb.SetFloat("_OutlineWidth", width);

            for (int i = 0; i < _outlineRenderers.Count; i++)
            {
                var r = _outlineRenderers[i];
                if (!r) continue;

                r.gameObject.SetActive(active);
                if (active) r.SetPropertyBlock(_mpb);
            }
        }
    }
}
