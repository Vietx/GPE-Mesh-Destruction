using System.Collections.Generic;
using UnityEngine;

public static class ShardBuilder
{
    public static GameObject SpawnShard(List<Vector2> poly, float thickness, Material mat, Transform parent = null)
    {
        int n = poly.Count;
        if (n < 3) return null;

        float half = thickness * 0.5f;

        // vertices: top (z=+half), bottom (z=-half)
        var verts = new Vector3[n * 2];
        for (int i = 0; i < n; i++)
        {
            Vector2 p = poly[i];
            verts[i]     = new Vector3(p.x, p.y,  half); // top
            verts[i + n] = new Vector3(p.x, p.y, -half); // bottom
        }

        // triangles
        var tris = new List<int>(n * 12);

        // top face (CCW)
        for (int i = 1; i < n - 1; i++)
            tris.AddRange(new int[] { 0, i, i + 1 });

        // bottom face (CW -> reverse)
        for (int i = 1; i < n - 1; i++)
            tris.AddRange(new int[] { n + 0, n + i + 1, n + i });

        // sides (each edge -> quad -> 2 tris)
        for (int i = 0; i < n; i++)
        {
            int a = i;
            int b = (i + 1) % n;
            int aTop = a;
            int bTop = b;
            int aBot = a + n;
            int bBot = b + n;

            tris.Add(aTop); tris.Add(bTop); tris.Add(bBot);
            tris.Add(aTop); tris.Add(bBot); tris.Add(aBot);
        }

        // UVs
        var uvs = new Vector2[verts.Length];
        Bounds2D(poly, out var min, out var size);
        for (int i = 0; i < n; i++)
        {
            Vector2 p = poly[i];
            var uv = new Vector2(
                size.x > 1e-6f ? (p.x - min.x) / size.x : 0f,
                size.y > 1e-6f ? (p.y - min.y) / size.y : 0f
            );
            uvs[i] = uv;         // top
            uvs[i + n] = uv;     // bottom
        }

        // create GameObject
        var go = new GameObject("Shard");
        if (parent) go.transform.SetParent(parent, false);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var mesh = new Mesh { name = "VoronoiShard" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
        if (mat) mr.sharedMaterial = mat;

        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = true;

        // go.AddComponent<Rigidbody>();
        return go;
    }

    static void Bounds2D(List<Vector2> poly, out Vector2 min, out Vector2 size)
    {
        min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var p in poly)
        {
            if (p.x < min.x) min.x = p.x;
            if (p.y < min.y) min.y = p.y;
            if (p.x > max.x) max.x = p.x;
            if (p.y > max.y) max.y = p.y;
        }
        size = max - min;
    }
}
