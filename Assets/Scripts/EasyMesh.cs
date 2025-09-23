using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class EasyMesh : MonoBehaviour
{
    Mesh mesh;
    private Vector3[] vert;
    private Vector2[] uvs;
    private Color[] colors;
    private int[] tris;

    [Range(0, 50)]
    public int xSize = 20, zSize = 20;
    [Range(0, 1)]
    public float noiseMultiplier = .3f;
    [Range(0, 100)]
    public float multiplier = 2f;
    public Gradient gradient;

    float maxTerrainHeight = 0f, minTerrainHeight = 0f;


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
        vert = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = Mathf.PerlinNoise(x * noiseMultiplier, z * noiseMultiplier) * multiplier;
                vert[i] = new Vector3(x, y, z);

                if (y > maxTerrainHeight)
                    maxTerrainHeight = y;

                if (y < minTerrainHeight)
                    minTerrainHeight = y;

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

        uvs = new Vector2[vert.Length];
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                uvs[i] = new Vector2((float)x / xSize, (float)z / zSize);
                // uvs[i] = new Vector2(x, z);
                i++;
            }
        }

        colors = new Color[vert.Length];
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float height = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, vert[i].y);
                colors[i] = gradient.Evaluate(height);
                i++;
            }
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vert;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();
    }

    /// <summary>
    /// slices plane into two pieces, with a horizontal line across x and z
    /// </summary>
    void TwoPieces()
    {
        //something
    }

    void OnDrawGizmos()
    {
        if (vert == null) return;

        for (int i = 0; i < vert.Length; i++)
        {
            Gizmos.DrawSphere(vert[i], .1f);
        }
    }
}

///
/// SOURCES USED
/// https://www.youtube.com/watch?v=lNyZ9K71Vhc
/// https://www.youtube.com/watch?v=64NblGkAabk
/// https://youtu.be/eJEpeUH1EMg
/// 