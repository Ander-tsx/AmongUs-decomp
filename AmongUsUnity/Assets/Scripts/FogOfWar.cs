using UnityEngine;

namespace AmongUsLan
{
    /// <summary>
    /// Line-of-sight fog: a visibility polygon (rays against wall colliders) marks the stencil buffer and a
    /// black overlay darkens everything outside it.
    /// </summary>
    public class FogOfWar : MonoBehaviour
    {
        const int Rays = 360;

        public Transform Target;
        public float Radius = 3.5f;
        public bool Active;

        Mesh _lightMesh, _overlayMesh;
        Vector3[] _verts;
        MeshRenderer _lightRenderer, _overlayRenderer;
        Camera _cam;

        static Shader Find(string n) => Shader.Find(n);

        public const int FogLayer = 6;

        public static FogOfWar Create(Camera cam)
        {
            var go = new GameObject("Fog");
            var f = go.AddComponent<FogOfWar>();
            f._cam = cam;
            f.Build();
            return f;
        }

        MeshRenderer MakeRenderer(string name, Mesh mesh, string shader, int order)
        {
            var go = new GameObject(name);
            go.layer = FogLayer;
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = new Material(Find(shader));
            r.sortingOrder = order;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        void Build()
        {
            _verts = new Vector3[Rays + 1];
            var tris = new int[Rays * 3];
            for (int i = 0; i < Rays; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = (i + 1) % Rays + 1;
            }
            _lightMesh = new Mesh { name = "FogLight" };
            _lightMesh.MarkDynamic();
            _lightMesh.vertices = _verts;
            _lightMesh.triangles = tris;

            _overlayMesh = new Mesh { name = "FogOverlay" };
            const float h = 60f;
            _overlayMesh.vertices = new[] { new Vector3(-h, -h), new Vector3(h, -h), new Vector3(h, h), new Vector3(-h, h) };
            _overlayMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _overlayMesh.bounds = new Bounds(Vector3.zero, new Vector3(h * 2, h * 2, 1));

            _lightRenderer = MakeRenderer("Light", _lightMesh, "AmongUsLan/FogLight", 900);
            _overlayRenderer = MakeRenderer("Overlay", _overlayMesh, "AmongUsLan/FogOverlay", 901);
        }

        void LateUpdate()
        {
            bool on = Active && Target != null;
            _lightRenderer.enabled = on;
            _overlayRenderer.enabled = on;
            if (!on) return;

            Vector2 origin = Target.position;
            _verts[0] = Vector3.zero;
            for (int i = 0; i < Rays; i++)
            {
                float a = i * Mathf.PI * 2f / Rays;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var hit = Physics2D.Raycast(origin, dir, Radius);
                float d = hit.collider != null ? hit.distance + 0.02f : Radius;
                _verts[i + 1] = new Vector3(dir.x * d, dir.y * d, 0);
            }
            _lightMesh.vertices = _verts;
            _lightMesh.RecalculateBounds();
            _lightRenderer.transform.position = new Vector3(origin.x, origin.y, 0);
            _overlayRenderer.transform.position = new Vector3(_cam.transform.position.x, _cam.transform.position.y, 0);
        }
    }
}
