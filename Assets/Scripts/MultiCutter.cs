using System.Collections.Generic;
using UnityEngine;

public class MultiPlaneCutter : MonoBehaviour
{
    public GameObject Target;

    public List<Transform> PlaneObjects = new();

    [Header("Shard Setup")]
    public Material ShardMaterial;         
    public bool DestroyOriginalAfterCut = true;
    public bool AddMeshCollider = true;
    public bool AddRigidbody = true;

    [Header("Physics")]
    public bool AddPhysics = true;
    public float ImpulseMin = 2f;
    public float ImpulseMax = 6f;

    [ContextMenu("-----Cut-----")]
    public void Cut()
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
}
