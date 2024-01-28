using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Facepunch.Flexbox
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class FlexGraphicTransform : UIBehaviour, IMeshModifier
    {
        [Range(0, 1)] public float OriginX = 0.5f;
        [Range(0, 1)] public float OriginY = 0.5f;

        public float TranslateX = 0;
        public float TranslateY = 0;
        public float ScaleX = 1;
        public float ScaleY = 1;
        public float Rotate = 0;

        private static readonly Dictionary<TextMeshProUGUI, FlexGraphicTransform> TextMeshProTransformers = new();
        private static readonly List<FlexGraphicTransform> Children = new();
        private static readonly List<TMP_SubMeshUI> SubMeshUIs = new();
        private static readonly List<Mesh> Meshes = new();
        private static readonly VertexHelper VertexHelper = new();
        private static readonly List<Vector3> Vertices = new();
        private static readonly List<int> Indices = new();
        private static readonly List<Color32> Colors = new();
        private static readonly List<Vector2> Uv0 = new();
        private static readonly List<Vector2> Uv1 = new();
        private static readonly List<Vector3> Normals = new();
        private static readonly List<Vector4> Tangents = new();

        static FlexGraphicTransform()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(obj =>
            {
                if (obj is TextMeshProUGUI tmp && TextMeshProTransformers.TryGetValue(tmp, out var transformer))
                {
                    transformer.ModifyTextMeshPro();
                }
            });
        }

        private FlexGraphicTransform _parent;
        private RectTransform _rt;
        private Graphic _graphic;
        private TextMeshProUGUI _textMeshPro;
        private CanvasRenderer _canvasRenderer;

        protected override void Awake()
        {
            base.Awake();

            UpdateParent();
            _rt = GetComponent<RectTransform>();
            _graphic = GetComponent<Graphic>();
            _textMeshPro = GetComponent<TextMeshProUGUI>();
            _canvasRenderer = GetComponent<CanvasRenderer>();
        }

        private void UpdateParent()
        {
            _parent = transform.parent != null
                ? transform.parent.GetComponent<FlexGraphicTransform>()
                : null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_textMeshPro != null)
            {
                TextMeshProTransformers.Add(_textMeshPro, this);
            }

            SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            if (_textMeshPro != null)
            {
                TextMeshProTransformers.Remove(_textMeshPro);
            }

            SetVerticesDirty();

            base.OnDisable();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetVerticesDirty();
            base.OnDidApplyAnimationProperties();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            UpdateParent();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif

        public void SetVerticesDirty()
        {
            if (_textMeshPro != null && _textMeshPro.textInfo?.meshInfo != null)
            {
                foreach (var info in _textMeshPro.textInfo.meshInfo)
                {
                    var mesh = info.mesh;
                    if (mesh != null)
                    {
                        mesh.Clear();
                        mesh.vertices = info.vertices;
                        mesh.uv = info.uvs0;
                        mesh.uv2 = info.uvs2;
                        mesh.colors32 = info.colors32;
                        mesh.normals = info.normals;
                        mesh.tangents = info.tangents;
                        mesh.triangles = info.triangles;
                    }
                }

                if (_canvasRenderer != null)
                {
                    _canvasRenderer.SetMesh(_textMeshPro.mesh);

                    _textMeshPro.GetComponentsInChildren(false, SubMeshUIs);
                    foreach (var sm in SubMeshUIs)
                    {
                        sm.canvasRenderer.SetMesh(sm.mesh);
                    }

                    SubMeshUIs.Clear();
                }

                _textMeshPro.havePropertiesChanged = true;
            }
            else if (_graphic != null)
            {
                _graphic.SetVerticesDirty();
            }

            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<FlexGraphicTransform>(out var childTransform) &&
                    childTransform.isActiveAndEnabled)
                {
                    childTransform.SetVerticesDirty();
                }
            }
        }

        public void ModifyMesh(Mesh mesh)
        {
            using (var vh = new VertexHelper(mesh))
            {
                ModifyMesh(vh);
                vh.FillMesh(mesh);
            }
        }

        private Matrix4x4 transformationMatrix
        {
            get
            {
                var pivotDelta = (new Vector2(OriginX, OriginY) - _rt.pivot) * _rt.rect.size;
                var pivotMatrix = Matrix4x4.Translate(new Vector3(pivotDelta.x, pivotDelta.y, 0));
                var transformMatrix = Matrix4x4.TRS(
                    new Vector3(TranslateX, TranslateY, 0),
                    Quaternion.Euler(0, 0, Rotate),
                    new Vector3(ScaleX, ScaleY, 1));
                return (pivotMatrix * transformMatrix) * pivotMatrix.inverse;
            }
        }

        public void ModifyMesh(VertexHelper vh)
        {
            var matrix = _parent != null
                ? _parent.transformationMatrix * transformationMatrix
                : transformationMatrix;

            var vt = default(UIVertex);
            var count = vh.currentVertCount;
            for (var i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vt, i);
                vt.position = matrix.MultiplyPoint(vt.position);
                vh.SetUIVertex(vt, i);
            }
        }

        private void ModifyTextMeshPro()
        {
            if (_textMeshPro == null || !isActiveAndEnabled)
            {
                return;
            }

            Meshes.Clear();
            foreach (var info in _textMeshPro.textInfo.meshInfo)
            {
                Meshes.Add(info.mesh);
            }

            foreach (var mesh in Meshes)
            {
                if (mesh != null)
                {
                    CopyIntoVertexHelper(mesh);
                    ModifyMesh(VertexHelper);
                    VertexHelper.FillMesh(mesh);
                }
            }

            if (_canvasRenderer != null)
            {
                _canvasRenderer.SetMesh(_textMeshPro.mesh);
                GetComponentsInChildren(false, SubMeshUIs);
                foreach (var sm in SubMeshUIs)
                {
                    sm.canvasRenderer.SetMesh(sm.mesh);
                }

                SubMeshUIs.Clear();
            }

            Meshes.Clear();
        }

        private static void CopyIntoVertexHelper(Mesh mesh)
        {
            VertexHelper.Clear();

            mesh.GetVertices(Vertices);
            mesh.GetIndices(Indices, 0);
            mesh.GetColors(Colors);
            mesh.GetUVs(0, Uv0);
            mesh.GetUVs(1, Uv1);
            mesh.GetNormals(Normals);
            mesh.GetTangents(Tangents);

            for (var i = 0; i < Vertices.Count; i++)
            {
                VertexHelper.AddVert(Vertices[i], Colors[i], Uv0[i], Uv1[i], Normals[i], Tangents[i]);
            }

            for (var i = 0; i < Indices.Count; i += 3)
            {
                VertexHelper.AddTriangle(Indices[i], Indices[i + 1], Indices[i + 2]);
            }
        }
    }
}
