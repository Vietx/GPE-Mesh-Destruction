using System.Collections.Generic;
using UnityEngine;

public class VornoiHelpers
{
    static List<Vector2> WalkCellBoundaries(VoronoiDiagram.Face face)
    {
        List<Vector2> loop = new();
        VoronoiDiagram.HalfEdge start = face.OuterComponent;
        VoronoiDiagram.HalfEdge edge = start;

        do
        {
            if (edge == null)
                break;

            loop.Add(edge.Origin.Point);

            if (edge.Next == null)
                break;
        }
        while (edge != start);

        return loop;
    }
}