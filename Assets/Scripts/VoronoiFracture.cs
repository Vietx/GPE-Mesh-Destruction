using System.Collections.Generic;
using UnityEngine;

public class VoronoiFracture : MonoBehaviour
{
    public GameObject Target;

    [Header("slicing stuff")]
    public List<Transform> PlaneObjects = new();
    public int amountOfPoints = 4;

    [Header("Shard Setup")]
    public Material ShardMaterial;
    public bool DestroyOriginalAfterCut = true;
    public bool AddMeshCollider = true;
    public bool AddRigidbody = true;

    [Header("Physics")]
    public bool AddPhysics = true;
    public float ImpulseMin = 2f;
    public float ImpulseMax = 6f;

    private Renderer _rend;
    private Bounds _bounds;
    private MeshFilter _mf;
    private List<Vector2> seedPoints;
    private List<Vector3> _seed3DPoints;

    [Header("Delete this later")]
    public bool threeD = false;

    void Start()
    {
        if (!Target) { Debug.Log("No Target assigned"); return; }

        _rend = Target.GetComponent<MeshRenderer>();
        _mf = Target.GetComponent<MeshFilter>();
        _bounds = _rend.bounds;
        Bounds lb = _mf.sharedMesh.bounds;

        Debug.Log($"Center: {_bounds.center}, Size: {_bounds.size}");

        float width = lb.size.x;
        float height = lb.size.z;
        float yyy = lb.size.y;

        if (!threeD)
        {
            seedPoints = SeedGenerator.GenerateSeeds2D(amountOfPoints, lb);
            for (int i = 0; i < seedPoints.Count; i++)
            {
                Vector3 Al = new Vector3(seedPoints[i].x, lb.center.y, seedPoints[i].y);

                for (int n = i + 1; n < seedPoints.Count; n++)
                {
                    Vector3 Bl = new Vector3(seedPoints[n].x, lb.center.y, seedPoints[n].y);

                    if ((Al - Bl).sqrMagnitude < 1e-6f) continue;//if two points are really close, skip
                    Plane pl = PerpBisectorPlaneXZ(Al, Bl);
                    Vector3 nW = Target.transform.TransformDirection(pl.normal).normalized;
                    Vector3 pW = Target.transform.TransformPoint(-pl.normal * pl.distance);
                    Plane pw = new Plane(nW, pW);

                    float s = Mathf.Max(_bounds.size.x, _bounds.size.z) * 2f;
                    Transform tf = CreatePlaneObject(pw, Target.transform, s);
                    PlaneObjects.Add(tf);
                }
            }
        }
        else
        {
            _seed3DPoints = SeedGenerator.GenerateSeeds3D(amountOfPoints, lb);
            List<Vector3> seedW = new();

            foreach (var p in _seed3DPoints)
            {
                seedW.Add(Target.transform.TransformPoint(p));
            }

            for (int i = 0; i < seedW.Count; i++)
            {
                Vector3 Aw = seedW[i];

                for (int n = i + 1; n < seedW.Count; n++)
                {
                    Vector3 Bw = seedW[n];

                    if ((Aw - Bw).sqrMagnitude < 1e-6f) continue;

                    Plane p = PerpBisectorPlane(Aw, Bw);

                    float s = Mathf.Max(_bounds.size.x, Mathf.Max(_bounds.size.y, _bounds.size.z)) * 2f;
                    Transform tf = CreatePlaneObject(p, Target.transform, s);
                    PlaneObjects.Add(tf);
                }
            }
        }
    }

    [ContextMenu("---Fracture---")]
    public void Fracture()
    {
        if (Target == null) { Debug.Log("no target set"); return; }

        MeshFilter mf = Target.GetComponent<MeshFilter>();
        MeshRenderer mr = Target.GetComponent<MeshRenderer>();
        if (!mf || mf.sharedMesh == null) { Debug.Log("Target has no mesh."); return; }

        //start with a duplicate of the mesh
        Mesh start = DuplicateMesh(mf.sharedMesh);
        List<Mesh> pieces = new List<Mesh> { start };

        foreach (Transform tf in PlaneObjects)
        {
            if (!tf) continue;

            //plane position
            Vector3 nW = tf.up.normalized;
            Vector3 pW = tf.position;

            //convert to local
            Vector3 nL = Target.transform.InverseTransformDirection(nW).normalized;
            Vector3 pL = Target.transform.InverseTransformPoint(pW);

            List<Mesh> next = new List<Mesh>(pieces.Count * 2);
            foreach (Mesh m in pieces)
            {
                if (!m) continue;

                Mesh[] slice = MeshSlicer.SliceMesh(m, pL, nL);
                if (slice != null && slice.Length == 2)
                {
                    next.Add(slice[0]);
                    next.Add(slice[1]);
                    Destroy(m);
                }
                else
                {
                    next.Add(m);
                }
            }
            pieces = next;
        }

        //hide original
        if (DestroyOriginalAfterCut) Target.SetActive(false);
        else if (mr) mr.enabled = false;

        //spawn shards
        Transform parent = Target.transform.parent;
        Material mat = ShardMaterial;

        foreach (var mesh in pieces)
        {
            GameObject go = new GameObject("Shard");
            go.transform.SetPositionAndRotation(Target.transform.position, Target.transform.rotation);
            go.transform.localScale = Target.transform.lossyScale;
            go.transform.SetParent(parent, true);

            MeshFilter shardMF = go.AddComponent<MeshFilter>(); shardMF.sharedMesh = mesh;
            MeshRenderer shardMR = go.AddComponent<MeshRenderer>(); shardMR.sharedMaterial = mat;

            if (AddMeshCollider)
            {
                var col = go.AddComponent<MeshCollider>();
                col.sharedMesh = mesh;
                col.convex = true;
            }

            if (AddRigidbody)
            {
                var rb = go.AddComponent<Rigidbody>();
                if (AddPhysics)
                {
                    Vector3 centerWorld = go.transform.TransformPoint(mesh.bounds.center);
                    Vector3 dir = (centerWorld - Target.transform.position).normalized;
                    rb.AddForce(dir * Random.Range(ImpulseMin, ImpulseMax), ForceMode.Impulse);
                }
            }
        }
    }

    static Mesh DuplicateMesh(Mesh src)
    {
        var m = new Mesh();
        m.SetVertices(src.vertices);
        m.SetNormals(src.normals);
        m.SetTangents(src.tangents);
        List<Vector4> uv = new List<Vector4>();
        src.GetUVs(0, uv);
        m.SetUVs(0, uv);
        m.subMeshCount = src.subMeshCount;

        for (int i = 0; i < src.subMeshCount; i++)
            m.SetTriangles(src.GetTriangles(i), i, true);

        m.colors = src.colors;
        m.RecalculateBounds();

        return m;
    }

    static Plane PerpBisectorPlane(Vector3 A, Vector3 B)
    {
        Vector3 n = (B - A).normalized;
        Vector3 m = (A + B) * .5f;
        return new Plane(n, m);
    }

    static Plane PerpBisectorPlaneXZ(Vector3 A, Vector3 B)
    {
        Vector3 n = B - A;
        n.y = 0f;
        n.Normalize();
        Vector3 m = (A + B) * 0.5f;
        return new Plane(n, m);
    }

    static Transform CreatePlaneObject(Plane p, Transform parent = null, float size = 1f)
    {
        Vector3 position = -p.normal * p.distance;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, p.normal);

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.transform.SetParent(parent, worldPositionStays: true);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = Vector3.one * (size / 10f);


        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));
        r.sharedMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        r.material.color = new Color(1, 0, 0, 0.2f);
        go.GetComponent<MeshCollider>().enabled = false;

        return go.transform;
    }

    void OnDrawGizmos()
    {
        if (!Target) return;

        var mf = Target.GetComponent<MeshFilter>();
        var mr = Target.GetComponent<MeshRenderer>();
        if (!mf || !mf.sharedMesh || !mr) return;

        Bounds bWorld = mr.bounds;

        if (seedPoints != null)
        {
            Gizmos.color = Color.cyan;
            Bounds lb = mf.sharedMesh.bounds;
            foreach (var s in seedPoints)
            {
                Vector3 local = new Vector3(s.x, lb.center.y, s.y);
                Vector3 world = Target.transform.TransformPoint(local);
                Gizmos.DrawSphere(world, 0.03f);
            }
        }

        if (_seed3DPoints != null)
        {
            Gizmos.color = Color.magenta;
            foreach (var s in _seed3DPoints)
            {
                Vector3 world = Target.transform.TransformPoint(s);
                Gizmos.DrawSphere(world, 0.03f);
            }
        }

        // renderer world bounds for context
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bWorld.center, bWorld.size);
    }
}
