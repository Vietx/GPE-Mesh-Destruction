using UnityEngine;

public class SphereGenerator : MonoBehaviour
{
    Mesh mesh;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] tris;

    [Range(0, 50)]
    public int xSize = 20, zSize = 20;
    [Range(0, 1)]
    public float noiseMultiplier = .3f;
    [Range(0, 100)]
    public float multiplier = 2f;


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
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = Mathf.PerlinNoise(x * noiseMultiplier, z * noiseMultiplier) * multiplier;
                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }

        tris = new int[xSize * zSize * 6];
        int vrt = 0;
        int trs = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                tris[trs + 0] = vrt + 0;
                tris[trs + 1] = vrt + xSize + 1;
                tris[trs + 2] = vrt + 1;
                tris[trs + 3] = vrt + 1;
                tris[trs + 4] = vrt + xSize + 1;
                tris[trs + 5] = vrt + xSize + 2;

                vrt++;
                trs += 6;
            }
            vrt++;
        }

        uvs = new Vector2[vertices.Length];
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                uvs[i] = new Vector2((float)x / xSize, (float)z / zSize);
                // uvs[i] = new Vector2(x, z);
                i++;
            }
        }

    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
    }

    void OnDrawGizmos()
    {
        if (vertices == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i], .02f);
        }
    }
}


///
/// SOURCES
/// Catlike coding
/// 
