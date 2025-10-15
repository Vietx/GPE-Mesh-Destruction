using System.Collections.Generic;
using UnityEngine;

static class VoronoiShardUtil
{
    public static List<Vector2> ExtractFacePolygon(VoronoiDiagram.Face face, int guardMax = 10000)
    {
        var poly = new List<Vector2>();
        var start = face?.OuterComponent;
        if (start == null) return poly;

        var he = start;
        int guard = 0;
        do
        {
            if (he.Origin != null) poly.Add(he.Origin.Point);
            he = he.Next;
            if (++guard > guardMax) break;
        }
        while (he != null && he != start);

        // Clean duplicates / tiny edges
        // poly = CleanPolygon(poly, 1e-5f);
        if (poly.Count >= 3 && !IsCCW(poly)) poly.Reverse();
        return poly;
    }

    static List<Vector2> CleanPolygon(List<Vector2> pts, float eps)
    {
        if (pts.Count == 0) return pts;
        var outp = new List<Vector2>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Count];
            if ((a - b).sqrMagnitude > eps * eps) outp.Add(a);
        }
        return outp;
    }

    static bool IsCCW(List<Vector2> p)
    {
        double s = 0;
        for (int i = 0; i < p.Count; i++)
        {
            var a = p[i];
            var b = p[(i + 1) % p.Count];
            s += (double)a.x * b.y - (double)a.y * b.x;
        }
        return s > 0;
    }
}
