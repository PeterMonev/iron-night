using UnityEngine;

namespace IronNight
{
    /// <summary>
    /// A searchlight beam: a long additive strip that always turns its face to the camera, bright and narrow at the
    /// lamp, wide and faint hundreds of metres up. The strip is rebuilt every frame from the lamp position and the
    /// beam direction, so it reads as a shaft of light from any angle the camera takes.
    /// </summary>
    public class LightShaft : MonoBehaviour
    {
        public static Camera cam;
        static Texture2D tex;
        const int N = 14;
        public float length = 380f, width0 = 2.2f, width1 = 16f;
        Vector3 origin, dir = Vector3.up; Mesh mesh; Material mat;
        readonly Vector3[] verts = new Vector3[(N + 1) * 2];

        void Awake()
        {
            var uv = new Vector2[(N + 1) * 2]; var tris = new int[N * 6];
            for (int i = 0; i <= N; i++) { float t = i / (float)N; uv[i * 2] = new Vector2(0f, t); uv[i * 2 + 1] = new Vector2(1f, t); }
            for (int i = 0; i < N; i++) { int b = i * 2; tris[i * 6] = b; tris[i * 6 + 1] = b + 2; tris[i * 6 + 2] = b + 1; tris[i * 6 + 3] = b + 1; tris[i * 6 + 4] = b + 2; tris[i * 6 + 5] = b + 3; }
            mesh = new Mesh { vertices = verts, uv = uv, triangles = tris };
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            if (tex == null)
            {
                // across: a hot core inside a soft halo, zero at the edges; along: fading with distance from the lamp
                tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float u = (x / 63f - 0.5f) * 2f, v = y / 63f;
                    float across = (0.7f * Mathf.Exp(-u * u / 0.09f) + 0.3f * Mathf.Exp(-u * u / 0.5f)) * Mathf.Clamp01((1f - Mathf.Abs(u)) * 3f);
                    float along = Mathf.Pow(1f - v, 1.6f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, across * along));
                }
                tex.Apply();
            }
            mat = new Material(Resources.Load<Material>("Additive")); mat.SetTexture("_BaseMap", tex); mat.SetColor("_BaseColor", new Color(0.9f, 1f, 1.3f, 0.3f));
            var mr = gameObject.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }

        public void Set(Vector3 lamp, Vector3 direction) { origin = lamp; dir = direction.normalized; }

        void LateUpdate()
        {
            if (cam == null) return;
            var cp = cam.transform.position;
            for (int i = 0; i <= N; i++)
            {
                float t = i / (float)N; var p = origin + dir * (t * length);
                var side = Vector3.Cross(dir, cp - p); if (side.sqrMagnitude < 1e-4f) side = cam.transform.right; side.Normalize();
                float w = Mathf.Lerp(width0, width1, t) * 0.5f;
                verts[i * 2] = p - side * w; verts[i * 2 + 1] = p + side * w;
            }
            mesh.vertices = verts; mesh.RecalculateBounds();
        }

        void OnDestroy() { Destroy(mesh); Destroy(mat); }
    }
}
