using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MainFA : MonoBehaviour
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

    //Fortune Algorithm stuff
    FortuneAlgorithm _fa;
    VoronoiDiagram _diagram;


    void Start()
    {
        if (!Target) { Debug.Log("No Target assigned"); return; }

        _rend = Target.GetComponent<MeshRenderer>();
        _mf = Target.GetComponent<MeshFilter>();
        _bounds = _rend.bounds;
        Bounds lb = _mf.sharedMesh.bounds;

        Debug.Log($"Center: {_bounds.center}, Size: {_bounds.size}");
    }

    //test function probably delete this later
    [ContextMenu("---spawn shards---")]
    public void SpawnShards()
    {
        if (_diagram == null) { Debug.Log("no diagram"); return; }
        if (_diagram.GetNbSites() == 0) { Debug.Log("no sites in diagram"); return; }

        Transform parent = Target.transform.parent;
        VoronoiShardSpawner.SpawnAllShards(_diagram, _bounds.size.y, ShardMaterial, parent);
    }

    [ContextMenu("---fracture mesh into cells---")]
    public void FractureMesh()
    {
        if (_diagram == null) { Debug.Log("no diagram"); return; }
        if (_diagram.GetNbSites() == 0) { Debug.Log("no sites in diagram"); return; }
        List<VoronoiDiagram.Face> faces = _diagram.GetAllFaces();
        Transform parent = Target.transform.parent;

        foreach (VoronoiDiagram.Face face in faces)
        {
            Debug.Log("we doing something alright");
            List<Vector2> cellBoundary = WalkCellBoundaries(face);
            if (cellBoundary.Count < 3)
            {
                Debug.Log("not enough");
                continue;
            }


            GameObject shard = new GameObject("Cell");
            shard.transform.parent = parent;
            shard.transform.SetPositionAndRotation(Target.transform.position, Target.transform.rotation);
            shard.transform.localScale = Target.transform.localScale;

            MeshFilter mf = shard.AddComponent<MeshFilter>();
            MeshRenderer mr = shard.AddComponent<MeshRenderer>();
        }
    }

    [ContextMenu("---regenerate---")]
    public void NewSeeds()
    {
        _fa = null;
        _diagram = null;

        if (!Target) { Debug.Log("No Target assigned"); return; }

        _rend = Target.GetComponent<MeshRenderer>();
        _mf = Target.GetComponent<MeshFilter>();
        _bounds = _rend.bounds;
        Bounds lb = _mf.sharedMesh.bounds;

        Debug.Log($"Center: {_bounds.center}, Size: {_bounds.size}");

        seedPoints = SeedGenerator.GenerateSeeds2D(amountOfPoints, lb);

        // construct points and diagram for algorithm
        _fa = new FortuneAlgorithm(seedPoints);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _fa.Construct();
        stopwatch.Stop();
        Debug.Log($"construction: {stopwatch.ElapsedMilliseconds} ms");

        //bound box a bit bigger than object bounds
        _fa.Bound(new Box { left = lb.min.x - 1f, bottom = lb.min.z - 1f, right = lb.max.x + 1f, top = lb.max.z + 1f });
        _diagram = _fa.GetDiagram();

        //intersect diagram with bounds of object
        stopwatch.Restart();
        bool valid = _diagram.Intersect(new Box { left = lb.min.x, bottom = lb.min.z, right = lb.max.x, top = lb.max.z });
        stopwatch.Stop();
        Debug.Log($"construction: {stopwatch.ElapsedMilliseconds} ms");
        Debug.Log("bool is " + valid);
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
                Gizmos.DrawSphere(world, 0.1f);
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

    // Helpers
    static List<Vector2> WalkCellBoundaries(VoronoiDiagram.Face face)
    {
        List<Vector2> loop = new();
        VoronoiDiagram.HalfEdge start = face.OuterComponent;
        VoronoiDiagram.HalfEdge edge = start;

        do
        {
            if (edge == null)
                break;

            loop.Add(edge.Origin.Point);

            if (edge.Next == null)
                break;

            edge = edge.Next;
        }
        while (edge != start);

        return loop;
    }
}
