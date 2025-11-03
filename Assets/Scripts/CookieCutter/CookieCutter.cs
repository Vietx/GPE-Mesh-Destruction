using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Cookiecutter : MonoBehaviour
{
    [Header("Target to cut")]
    public GameObject target;

    [Header("Setup")]
    //amount of rings to create to simulate the spiderweb effect
    public int nrOfRings = 2;
    //vertical slices to make easy glass shards
    public int nrOfCutsPerImpact = 4;
    //point of bullet impact in local space of object
    public Vector3 impactPoint = Vector3.zero;
    public Material ShardMaterial;
    public float offset = .3f;

    [Header("Options")]
    public bool AddMeshCollider = true;
    public bool AddRigidbody = false;
    public bool CutWithSeperateScript = false;
    public bool DestroyOriginalAfterCut = true;
    public bool randomNrOfImpacts = true;
    public int upperRange = 5, lowerRange = 3;

    [Header("Physics")]
    public bool AddPhysics = false;
    public float ImpulseMin = 2f;
    public float ImpulseMax = 6f;

    [Header("Raycasting")]
    public LayerMask glassLayerMask;
    Camera cam;

    List<Plane> planeObjects = new();
    List<Plane> bisectingPlanes = new();
    private List<Vector2> seedPoints = new();
    List<GameObject> createdShards = new();

    float minAreaThreshold = 1e-4f; //due to overhead of creating shards, some spawn without any area, delete these

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //raycast to get impact point
            ShootRayCast();
        }

        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);


        if (Input.GetKeyDown(KeyCode.LeftShift))
            AddRigidbody = !AddRigidbody;


        if (Input.GetKeyDown(KeyCode.LeftControl))
            CutWithSeperateScript = !CutWithSeperateScript;

    }

    void ShootRayCast()
    {
        if (randomNrOfImpacts) nrOfCutsPerImpact = Random.Range(lowerRange, upperRange);

        createdShards.Clear();
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, glassLayerMask))
        {
            target = hit.collider.gameObject;
            ShardMaterial = target.GetComponent<MeshRenderer>().sharedMaterial;
            impactPoint = target.transform.InverseTransformPoint(hit.point);
            GenerateCuttingPlanes(nrOfCutsPerImpact, target.transform, impactPoint);
            CreateCutsFromImpactPoint();
        }
    }

    void CreateCutsFromImpactPoint()
    {
        if (target == null) { Debug.Log("no target set"); return; }

        MeshFilter mf = target.GetComponent<MeshFilter>();
        MeshRenderer mr = target.GetComponent<MeshRenderer>();
        if (!mf || mf.sharedMesh == null) { Debug.Log("Target has no mesh."); return; }

        //start with a duplicate of the mesh
        Mesh start = DuplicateMesh(mf.sharedMesh);
        List<Mesh> pieces = new List<Mesh> { start };

        Vector3 worldImpact = target.transform.TransformPoint(impactPoint);

        foreach (var plane in planeObjects)
        {
            if (!target) continue;

            //plane position
            Vector3 nW = plane.normal.normalized;
            Vector3 pW = -plane.distance * plane.normal;

            //convert to local
            Vector3 nL = target.transform.InverseTransformDirection(nW).normalized;
            Vector3 pL = target.transform.InverseTransformPoint(pW);


            List<Mesh> next = new List<Mesh>(pieces.Count + 4);//maybe change back to *2 if too many meshes are created, needs testing
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

        //spawn shards
        Transform parent = target.transform.parent;
        Material mat = ShardMaterial;

        foreach (var mesh in pieces)
        {
            GameObject go = new GameObject("Shard");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = target.transform.localPosition;
            go.transform.localRotation = target.transform.localRotation;
            go.transform.localScale = target.transform.localScale;
            go.layer = LayerMask.NameToLayer("Glass");

            MeshFilter shardMF = go.AddComponent<MeshFilter>(); shardMF.sharedMesh = mesh;
            MeshRenderer shardMR = go.AddComponent<MeshRenderer>(); shardMR.sharedMaterial = mat;

            if (CheckArea(go) < minAreaThreshold)
            {
                Destroy(go);
            }
            else
            {
                if (AddMeshCollider)
                {
                    MeshCollider col = go.AddComponent<MeshCollider>();
                    col.sharedMesh = mesh;
                }

                if (CutWithSeperateScript)
                {
                    CrackCutter crack = go.AddComponent<CrackCutter>();
                    crack.ShardMaterial = ShardMaterial;
                    crack.originalImpactPoint = worldImpact;
                    crack.AddMeshCollider = AddMeshCollider;
                    crack.AddRigidbody = AddRigidbody;
                }

                createdShards.Add(go);
            }
        }

        if (!DestroyOriginalAfterCut)
            target.SetActive(false);
        else
            Destroy(target);

        if (!CutWithSeperateScript)
            SecondPass();
    }

    void SecondPass()
    {
        foreach (var shard in createdShards)
        {
            SecondPassCut(shard);
        }
    }

    void SecondPassCut(GameObject shard)
    {
        MeshFilter mf = shard.GetComponent<MeshFilter>();
        if (!mf || mf.sharedMesh == null) { Debug.Log("Target has no mesh."); return; }

        //start with a duplicate of the mesh
        Mesh start = DuplicateMesh(mf.sharedMesh);
        List<Mesh> pieces = new List<Mesh> { start };

        Vector3 impactWorld = shard.transform.TransformPoint(impactPoint);
        GenerateBisectingPlanes(nrOfRings, shard.transform, impactWorld);

        foreach (var plane in bisectingPlanes)
        {
            //plane position
            Vector3 nW = plane.normal.normalized;
            Vector3 pW = -plane.distance * plane.normal;

            //convert to local
            Vector3 nL = shard.transform.InverseTransformDirection(nW).normalized;
            Vector3 pL = shard.transform.InverseTransformPoint(pW);

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

        //spawn shards
        Transform parent = shard.transform.parent;
        Material mat = ShardMaterial;

        foreach (var mesh in pieces)
        {
            GameObject go = new GameObject("Shard1");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = target.transform.localPosition;
            go.transform.localRotation = target.transform.localRotation;
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

        if (!DestroyOriginalAfterCut)
            shard.SetActive(false);
        else
            Destroy(shard);
    }

    void GenerateCuttingPlanes(int nrOfCuts, Transform tf, Vector3 impact)
    {
        planeObjects.Clear();

        Vector3 impactWorld = tf.TransformPoint(impact);

        Vector3 n = tf.up.normalized;
        Vector3 refAxis = Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up; //if normal is vertical, pick right instead
        Vector3 t1 = Vector3.Normalize(Vector3.Cross(refAxis, n));
        Vector3 t2 = Vector3.Normalize(Vector3.Cross(n, t1));

        for (int i = 0; i < nrOfCuts; i++)
        {
            float theta = i * (Mathf.PI / nrOfCuts);//angle
            float randomOffset = Random.Range(-offset, offset);
            float a = theta + randomOffset;
            Vector3 radial = Mathf.Cos(a) * t1 + Mathf.Sin(a) * t2;

            planeObjects.Add(new Plane(radial.normalized, impactWorld));
        }
    }

    void GenerateBisectingPlanes(int rings, Transform tf, Vector3 impact)
    {
        bisectingPlanes.Clear();
        MeshFilter mf = tf.GetComponent<MeshFilter>();

        Vector3 iw = impact;
        Vector3 pNormal = tf.up.normalized;
        Plane pane = new Plane(pNormal, iw);

        // seedPoints = SeedGenerator.GenerateSeeds2D(rings, mf.sharedMesh.bounds);
        seedPoints = SeedGenerator.GenerateSeeds2DAlongMiddle(rings, mf.sharedMesh.bounds);

        foreach (var seed in seedPoints)
        {
            var lb = mf.sharedMesh.bounds;
            Vector3 sl = new Vector3(seed.x, lb.center.y, seed.y);
            Vector3 sw = tf.TransformPoint(sl);
            sw = pane.ClosestPointOnPlane(sw);

            Plane pb = PerpBisectorPlane(iw, sw, pNormal);
            bisectingPlanes.Add(pb);
        }
    }

    //helpers
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
        return areaEstimate;
    }

    static Plane PerpBisectorPlane(Vector3 A, Vector3 B, Vector3 paneNormal)
    {
        Vector3 n = B - A;
        n -= Vector3.Dot(n, paneNormal) * paneNormal;
        n.Normalize();
        Vector3 m = (A + B) * 0.5f;
        return new Plane(n, m);
    }

    void OnDrawGizmosSelected()
    {
        if (!target) return;
        Gizmos.color = Color.red;
        Vector3 worldImpact = target.transform.TransformPoint(impactPoint);
        Gizmos.DrawSphere(worldImpact, 0.07f);
    }
}