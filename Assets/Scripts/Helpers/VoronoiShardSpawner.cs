using System.Collections.Generic;
using UnityEngine;

public static class VoronoiShardSpawner
{
    public static void SpawnAllShards(VoronoiDiagram diagram, float thickness, Material mat, Transform parent = null, float minArea = 1e-6f)
    {
        int n = diagram.GetNbSites();
        for (int i = 0; i < n; i++)
        {
            var face = diagram.GetFace(i);
            if (face == null || face.OuterComponent == null) continue;

            var poly = VoronoiShardUtil.ExtractFacePolygon(face);
            if (poly.Count < 3) continue;
            if (PolygonArea(poly) < minArea) continue;

            ShardBuilder.SpawnShard(poly, thickness, mat, parent);
        }
    }

    static float PolygonArea(List<Vector2> p)
    {
        double a = 0;
        for (int i = 0; i < p.Count; i++)
        {
            var q = p[(i + 1) % p.Count];
            a += (double)p[i].x * q.y - (double)p[i].y * q.x;
        }
        return Mathf.Abs((float)(a * 0.5));
    }
}