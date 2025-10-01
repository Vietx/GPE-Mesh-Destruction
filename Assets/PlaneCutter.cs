using UnityEngine;

public class PlaneCutter : MonoBehaviour
{
    [SerializeField]
    private GameObject _target;
    private MeshFilter _targetMesh;
    private Vector3 _normal;
    private Vector3 _origin;

    void Start()
    {
        _normal = gameObject.transform.up;
        Debug.Log("normal:" + _normal);
        _origin = gameObject.transform.position;
        Debug.Log("origin:" + _origin);
    }

    void FixedUpdate()
    {
        _normal = gameObject.transform.up;
        _origin = gameObject.transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);
        _target = other.gameObject;
        _targetMesh = other.GetComponentInChildren<MeshFilter>();
    }

    void OnTriggerExit(Collider other)
    {
        _target = null;
        _targetMesh = null;
    }

    public void SliceMesh()
    {
        Transform t = _target.transform;
        Vector3 originLocal = t.InverseTransformPoint(_origin);
        Vector3 normalLocal = t.InverseTransformDirection(_normal).normalized;
        Mesh[] meshes = MeshSlicer.SliceMesh(_targetMesh.sharedMesh, originLocal, normalLocal);
        for (int i = 0; i < meshes.Length; i++)
        {
            Mesh mesh = meshes[i];
            GameObject submesh = Instantiate(_target.gameObject);
            submesh.transform.position += 2 * transform.right;
            submesh.GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
