using System.Security.Cryptography;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CylinderMesh : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    Vector2[] uvs;
    int[] triangles;

    [Range(1, 100)]
    public int numOfSides = 4;
    [Range(1, 100)]
    public float radius = 1f;
    [Range(1, 10)]
    public int cylinderHeight = 4;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        CreateShape();
    }

    void Update()
    {
        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        CreateVertices();
        CreateTriangles();
        GenerateUvCoords();

    }

    void CreateVertices()
    {
        vertices = new Vector3[2 * numOfSides + 2];

        //center bottom vertex and top vertex
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(0, cylinderHeight, 0);

        float angle = 2 * Mathf.PI / numOfSides;
        //bottom ring
        for (int i = 0; i < numOfSides; i++)
        {
            float x = Mathf.Cos(i * angle) * radius;
            float z = Mathf.Sin(i * angle) * radius;
            vertices[i + 2] = new Vector3(x, 0, z);
        }

        //top ring
        for (int i = 0; i < numOfSides; i++)
        {
            float x = Mathf.Cos(i * angle) * radius;
            float z = Mathf.Sin(i * angle) * radius;
            vertices[i + 2 + numOfSides] = new Vector3(x, cylinderHeight, z);
        }
    }

    void CreateTriangles()
    {
        triangles = new int[4 * numOfSides * 3];
        int t = 0;
        int bottomStart = 2;
        int topStart = numOfSides + 2;

        //create bottom cap
        for (int i = 0; i < numOfSides; i++)
        {
            int curr = bottomStart + i;
            int next = bottomStart + ((i + 1) % numOfSides);

            triangles[t++] = 0;
            triangles[t++] = curr;
            triangles[t++] = next;
        }

        //create top cap
        for (int i = 0; i < numOfSides; i++)
        {
            int curr = topStart + i;
            int next = topStart + ((i + 1) % numOfSides);

            triangles[t++] = 1;
            triangles[t++] = next;
            triangles[t++] = curr;
        }

        //create sides
        for (int i = 0; i < numOfSides; i++)
        {
            int next = (i + 1) % numOfSides;


            triangles[t++] = bottomStart + i;
            triangles[t++] = topStart + i;
            triangles[t++] = topStart + next;
            triangles[t++] = bottomStart + i;
            triangles[t++] = topStart + next;
            triangles[t++] = bottomStart + next;
        }
    }

    void GenerateUvCoords()
    {
        uvs = new Vector2[vertices.Length];
        int bottomStart = 2;
        int topStart = numOfSides + 2;

        for (int i = 0; i < numOfSides; i++)
        {
            float u = i / (float)numOfSides;
            if (i == numOfSides - 1) u = 1f;
            uvs[bottomStart + i] = new Vector2(u, 0f);
            uvs[topStart + i] = new Vector2(u, 1f);
        }
        uvs[0] = new Vector2(0.5f, 0.5f);
        uvs[1] = new Vector2(0.5f, 0.5f);

        //no clue why one face is right and the rest is stretched, due to time constraints im leaving this uv part as it is now, halfworking.
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }


    void OnDrawGizmos()
    {
        if (vertices == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i], .05f);
        }
    }
}
