using System.Collections.Generic;
using UnityEngine;

public static class SeedGenerator {
    public static List<Vector2> GenerateSeeds2D(int count, Bounds localBounds) {
        var seeds = new List<Vector2>(count);
        var c = localBounds.center;
        var e = localBounds.extents;
        for (int i = 0; i < count; i++) {
            float x = Random.Range(c.x - e.x, c.x + e.x);
            float z = Random.Range(c.z - e.z, c.z + e.z);
            seeds.Add(new Vector2(x, z));
        }
        return seeds;
    }

    public static List<Vector3> GenerateSeeds3D(int count, Bounds localBounds)
    {
        var seeds = new List<Vector3>(count);
        var c = localBounds.center;
        var e = localBounds.extents;
        for (int i = 0; i < count; i++)
        {
            seeds.Add(new Vector3(
                Random.Range(c.x - e.x, c.x + e.x),
                Random.Range(c.y - e.y, c.y + e.y),
                Random.Range(c.z - e.z, c.z + e.z)
            ));
        }
        return seeds;
    }

    public static List<Vector2> GenerateSeeds2DAlongMiddle(int count, Bounds localBounds)
    {
        var seeds = new List<Vector2>(count);
        var c = localBounds.center;
        var e = localBounds.extents;

        for (int i = 0; i < count; i++)
        {
            float u1 = 1f - Random.value;
            float u2 = 1f - Random.value;
            float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);

            // Scale Gaussian: 0 = center, 1 = edge
            float spread = 0.4f; // smaller = tighter cluster to center
            float x = Mathf.Clamp(c.x + randStdNormal * e.x * spread, c.x - e.x, c.x + e.x);
            float z = Random.Range(c.z - e.z, c.z + e.z); // keep z uniform

            seeds.Add(new Vector2(x, z));
        }

        return seeds;
    }
}