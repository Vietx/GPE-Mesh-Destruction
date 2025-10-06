using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class CreateQuad : MonoBehaviour
{
    Mesh mesh;
    private Vector3[] vert;
    private Vector2[] uvs;
    private int[] tris;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        vert = new Vector3[]
        {
            new Vector3 (0,0,0),
            new Vector3 (0,0,1),
            new Vector3 (1,0,0),
            new Vector3 (1,0,1)
        };

        tris = new int[]
        {
            0, 1, 2,
            1, 3, 2
        };
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vert;
        mesh.triangles = tris;
    }
}
