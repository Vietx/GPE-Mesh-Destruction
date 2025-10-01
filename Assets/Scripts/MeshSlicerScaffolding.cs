using UnityEngine;

public class MeshSlicerScaffolding : MonoBehaviour
{
    [SerializeField]
    private MeshFilter _meshFilter;
    [SerializeField]
    private Vector3 _origin;
    [SerializeField]
    private Vector3 _normal;

    /// <summary>
    /// slice a mesh
    /// </summary>
    public void SliceMesh()
    {
        Mesh[] meshes = MeshSlicer.SliceMesh(_meshFilter.sharedMesh, _origin, _normal);
        for (int i = 0; i < meshes.Length; i++)
        {
            Mesh mesh = meshes[i];
            GameObject submesh = Instantiate(this.gameObject);
            submesh.gameObject.transform.position += 2 * transform.right;
            submesh.GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }

    /// <summary>
    /// show cutting plane in gizmos
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        //We construct new gizmos matrix taking our _normal as forward position
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.LookRotation(_normal), Vector3.one);
        //We draw cubes that will now represent our slicing plane
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawCube(_origin, new Vector3(2, 2, 0.01f));
        Gizmos.color = new Color(0, 1, 0, 1f);
        Gizmos.DrawWireCube(_origin, new Vector3(2, 2, 0.01f));
        //We set matrix to our object matrix and draw all of the normals.
        //It will be especially usefull after we start
        //slicing mesh and have to check
        //if all faces where created correctly 
        Gizmos.color = Color.blue;
        Gizmos.matrix = transform.localToWorldMatrix;
        for (int i = 0; i < _meshFilter.sharedMesh.normals.Length; i++)
        {
            Vector3 normal = _meshFilter.sharedMesh.normals[i];
            Vector3 vertex = _meshFilter.sharedMesh.vertices[i];
            Gizmos.DrawLine(vertex, vertex + normal);
        }
    }
}

/*
https://medium.com/@hesmeron/mesh-slicing-in-unity-740b21ffdf84
*/