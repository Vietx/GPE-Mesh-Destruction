
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Cookiecutter : MonoBehaviour
{
    [Header("Target to cut")]
    public GameObject target;

    [Header("Setup")]
    //amount of rings to create to simulate the spiderweb effect
    public float nrOfRings = 2;
    //vertical slices to make easy glass shards
    public int nrOfCutsPerImpact = 4;
    //point of bullet impact in local space of object
    public Vector3 impactPoint = Vector3.zero;
    public Material ShardMaterial;

    [Header("Options")]
    public bool AddMeshCollider = true;
    public bool AddRigidbody = false;
    public bool CutWithSeperateScript = false;
    public bool DestroyOriginalAfterCut = true;
    public bool secondCutPassTesting = false;

    [Header("Physics")]
    public bool AddPhysics = false;
    public float ImpulseMin = 2f;
    public float ImpulseMax = 6f;

    [Header("Raycasting")]
    public LayerMask glassLayerMask;
    Camera cam;

    List<Plane> planeObjects = new();
    List<Plane> radialCuttingPlanes = new();
    Plane angledCutter;
    List<GameObject> createdShards = new();

    float minAreaThreshold = 0.0001f; //due to overhead of creating shards, some spawn without any area, delete these

    void Awake()
    {
        cam = Camera.main;
    }

    void Start()
    {
        // GenerateCuttingPlanes(nrOfCutsPerImpact, target.transform, impactPoint);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //raycast to get impact point
            ShootRayCast();
        }
    }
    
    void ShootRayCast()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, glassLayerMask))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow, 1f);
            Debug.Log("hit glass at: " + hit.point);
            target = hit.collider.gameObject;
            ShardMaterial = target.GetComponent<MeshRenderer>().sharedMaterial;
            impactPoint = target.transform.InverseTransformPoint(hit.point);
            GenerateCuttingPlanes(nrOfCutsPerImpact, target.transform, impactPoint);
            CreateCutsFromImpactPoint();
        }
        else
        { 
            Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.white, 0.5f); 
            Debug.Log("Did not Hit"); 
        }
    }

    [ContextMenu("Do Stuff")]
    public void DoStuff()
    {
        GenerateCuttingPlanes(nrOfCutsPerImpact, target.transform, impactPoint);
    }
    
    [ContextMenu("Cut Mesh")]
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
                    if (!secondCutPassTesting)
                    {
                        next.Add(slice[0]);
                        next.Add(slice[1]);
                        Destroy(m);
                    }
                    else
                    {
                        Debug.Log("second cut pass testing");
                        GenerateRadialCuttingPlane(target.transform, impactPoint, 0.3f);
                        Mesh[] radialSliceA = MeshSlicer.SliceMesh(slice[0], target.transform.InverseTransformPoint(angledCutter.normal * -angledCutter.distance), angledCutter.normal);
                        Mesh[] radialSliceB = MeshSlicer.SliceMesh(slice[1], target.transform.InverseTransformPoint(angledCutter.normal * -angledCutter.distance), angledCutter.normal);
                        if (radialSliceA != null && radialSliceA.Length == 2)
                        {
                            next.Add(radialSliceA[0]);
                            next.Add(radialSliceA[1]);
                            Destroy(slice[0]);
                        }
                        else
                        {
                            next.Add(slice[0]);
                        }
                        if (radialSliceB != null && radialSliceB.Length == 2)
                        {
                            next.Add(radialSliceB[0]);
                            next.Add(radialSliceB[1]);
                            Destroy(slice[1]);
                        }
                        else
                        {
                            next.Add(slice[1]);
                        }
                    }
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
            go.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            go.transform.localScale = target.transform.lossyScale;
            go.transform.SetParent(parent, true);
            go.layer = LayerMask.NameToLayer("Glass");

            MeshFilter shardMF = go.AddComponent<MeshFilter>(); shardMF.sharedMesh = mesh;
            MeshRenderer shardMR = go.AddComponent<MeshRenderer>(); shardMR.sharedMaterial = mat;

            if (CutWithSeperateScript)
            {
                CrackCutter crack = go.AddComponent<CrackCutter>();
                crack.ShardMaterial = ShardMaterial;
                crack.originalImpactPoint = worldImpact;
                crack.AddMeshCollider = AddMeshCollider;
                crack.AddRigidbody = AddRigidbody;
            }
            
            if (AddMeshCollider)
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
                    Vector3 dir = (centerWorld - target.transform.position).normalized;
                    rb.AddForce(dir * Random.Range(ImpulseMin, ImpulseMax), ForceMode.Impulse);
                }
            }

            if (CheckArea(go) < minAreaThreshold)
                Destroy(go);

            createdShards.Add(go);
        }

        if (!DestroyOriginalAfterCut)
            target.SetActive(false);
        else
            Destroy(target);
    }

    void GenerateCuttingPlanes(int nrOfCuts, Transform tf, Vector3 impact)
    {
        planeObjects.Clear();

        MeshFilter mf = tf.GetComponent<MeshFilter>();
        Vector3 impactWorld = tf.TransformPoint(impact);

        Vector3 n = tf.up.normalized;
        Vector3 refAxis = Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up; //if normal is vertical, pick right instead
        Vector3 t1 = Vector3.Normalize(Vector3.Cross(refAxis, n));
        Vector3 t2 = Vector3.Normalize(Vector3.Cross(n, t1));

        for (int i = 0; i < nrOfCuts; i++)
        {
            float theta = i * (Mathf.PI / nrOfCuts);//angle
            Vector3 radial = Mathf.Cos(theta) * t1 + Mathf.Sin(theta) * t2;

            planeObjects.Add(new Plane(radial.normalized, impactWorld));
        }
    }

    void GenerateRadialCuttingPlane(Transform tf, Vector3 impact, float r)
    {
        // radialCuttingPlanes.Clear();

        // MeshFilter mf = tf.GetComponent<MeshFilter>();
        // Vector3 impactWorld = tf.TransformPoint(impact);
        // Vector3 n = tf.up.normalized;
        // Vector3 centerWorld = tf.TransformPoint(mf.sharedMesh.bounds.center);
        // Vector3 v = centerWorld - impactWorld;
        // Vector3 vPane = v - Vector3.Dot(v, n) * n;
        // Vector3 u = vPane.normalized;
        // Vector3 p = impactWorld + r * u;

        // Vector3 normal = u;

        // angledCutter = new Plane(normal, p);
        Debug.Log("this does nothing atm");
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

    void OnDrawGizmosSelected()
    {
        if (!target) return;
        Gizmos.color = Color.red;
        Vector3 worldImpact = target.transform.TransformPoint(impactPoint);
        Gizmos.DrawSphere(worldImpact, 0.07f);
    }
}