
using System.Collections.Generic;
using UnityEngine;

public class CrackCutter : MonoBehaviour
{
    public int cuts = 2;
    public Vector3 originalImpactPoint;
    public Material ShardMaterial;

    List<Plane> planeObjects = new();

    float minAreaThreshold = 0.0001f;
    private List<Vector2> seedPoints;
    bool isCut = false;

    public bool AddMeshCollider = false;
    public bool AddRigidbody = false;
    public bool CutWithSeperateScript = false;
    public bool DestroyOriginalAfterCut = true;

    public bool AddPhysics = false;
    public float ImpulseMin = 2f;
    public float ImpulseMax = 6f;

    void Start()
    {
        if (isCut) return;
        GenerateCuttingPlanes(cuts, transform, originalImpactPoint);
        CutMesh();
    }
    [ContextMenu("cut")]
    void CutMesh()
    {
        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        if (!mf || mf.sharedMesh == null) { Debug.Log("Target has no mesh."); return; }

        //start with a duplicate of the mesh
        Mesh start = DuplicateMesh(mf.sharedMesh);
        List<Mesh> pieces = new List<Mesh> { start };

        foreach (var plane in planeObjects)
        {
            //plane position
            Vector3 nW = plane.normal.normalized;
            Vector3 pW = -plane.distance * plane.normal;

            //convert to local
            Vector3 nL = gameObject.transform.InverseTransformDirection(nW).normalized;
            Vector3 pL = gameObject.transform.InverseTransformPoint(pW);

            List<Mesh> next = new List<Mesh>(pieces.Count + 4);
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
        gameObject.SetActive(false);

        //spawn shards
        Transform parent = gameObject.transform.parent;
        Material mat = ShardMaterial;

        foreach (var mesh in pieces)
        {
            GameObject go = new GameObject("Shard1");
            go.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
            go.transform.localScale = gameObject.transform.lossyScale;
            go.transform.SetParent(parent, true);
            go.layer = LayerMask.NameToLayer("Glass");

            MeshFilter shardMF = go.AddComponent<MeshFilter>(); shardMF.sharedMesh = mesh;
            MeshRenderer shardMR = go.AddComponent<MeshRenderer>(); shardMR.sharedMaterial = mat;

            float area = CheckArea(go);
            bool areaBigEnough = area >= 1f ? true : false; //maybe despawn these after a while TODO
            if (area < minAreaThreshold)
            {
                Destroy(go);
                continue;
            }
            
            if (AddMeshCollider && areaBigEnough)
            {
                MeshCollider col = go.AddComponent<MeshCollider>();
                col.sharedMesh = mesh;
                col.convex = true;
            }

            if (AddRigidbody)
            {
                Rigidbody rb = go.AddComponent<Rigidbody>();
                if (AddPhysics)
                {
                    Vector3 centerWorld = go.transform.TransformPoint(mesh.bounds.center);
                    Vector3 dir = (centerWorld - go.transform.position).normalized;
                    rb.AddForce(dir * Random.Range(ImpulseMin, ImpulseMax), ForceMode.Impulse);
                }
            }
        }
        isCut = true;//not needed atm
    }

    void GenerateCuttingPlanes(int nrOfCuts, Transform tf, Vector3 impact)
    {
        planeObjects.Clear();
        MeshFilter mf = tf.GetComponent<MeshFilter>();
        Vector3 iw = impact;

        Vector3 pNormal = tf.up.normalized;
        Plane pane = new Plane(pNormal, iw);

        seedPoints = SeedGenerator.GenerateSeeds2D(nrOfCuts, mf.sharedMesh.bounds);

        foreach (var seed in seedPoints)
        {
            var lb = mf.sharedMesh.bounds;
            Vector3 sl = new Vector3(seed.x, lb.center.y, seed.y);
            Vector3 sw = tf.TransformPoint(sl);
            sw = pane.ClosestPointOnPlane(sw);
            Plane pb = PerpBisectorPlane(iw, sw, pNormal);
            planeObjects.Add(pb);
        }
    }

    //helpers
    static Plane PerpBisectorPlane(Vector3 A, Vector3 B, Vector3 paneNormal)
    {
        Vector3 n = B - A;
        n -= Vector3.Dot(n, paneNormal) * paneNormal;
        n.Normalize();
        Vector3 m = (A + B) * 0.5f;
        return new Plane(n, m);
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
    public float CheckArea(GameObject t)
    {
        MeshFilter mf = t.GetComponent<MeshFilter>();
        Bounds b = mf.sharedMesh.bounds;
        Vector3 size = b.size;
        float areaEstimate = 2f * (size.x * size.y + size.y * size.z + size.z * size.x);
        Debug.Log("Area estimate: " + areaEstimate);
        return areaEstimate;
    }

    [ContextMenu("Check Area")]
    public float Areacheck()
    {
        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        Bounds b = mf.sharedMesh.bounds;
        Vector3 size = b.size;
        float areaEstimate = 2f * (size.x * size.y + size.y * size.z + size.z * size.x);
        Debug.Log("Area estimate: " + areaEstimate);
        return areaEstimate;
    }

    void OnDrawGizmosSelected()
    {
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        Gizmos.color = Color.cyan;
        Bounds lb = mf.sharedMesh.bounds;
        foreach (var s in seedPoints)
        {
            Vector3 local = new Vector3(s.x, lb.center.y, s.y);
            Vector3 world = transform.TransformPoint(local);
            Gizmos.DrawSphere(world, 0.07f);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward);
    }
}