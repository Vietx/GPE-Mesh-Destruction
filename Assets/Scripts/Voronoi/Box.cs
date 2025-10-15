using UnityEngine;

public class Box
{
    public enum Side { Left = 0, Bottom = 1, Right = 2, Top = 3 }

    public struct Intersection
    {
        public Side side;
        public Vector2 point;
    }

    public double left, bottom, right, top;
    const double EPSILON = 1e-6;

    public bool Contains(Vector2 point)
    {
        return point.x >= left - EPSILON && point.x <= right + EPSILON &&
        point.y >= bottom - EPSILON && point.y <= top + EPSILON;
    }

    public Intersection GetFirstIntersection(Vector2 origin, Vector2 direction)
    {
        Intersection intersection = new();
        double bestT = double.PositiveInfinity;

        // Right /  Left
        if (Mathf.Abs(direction.x) > 0f)
        {
            double t = (right - origin.x) / direction.x;
            if (t > EPSILON)
            {
                Vector2 p = origin + (float)t * direction;
                if (p.y >= bottom - EPSILON && p.y <= top + EPSILON && t < bestT)
                {
                    bestT = t;
                    intersection.side = Side.Right;
                    intersection.point = p;
                }
            }
            t = (left - origin.x) / direction.x;
            if (t > EPSILON)
            {
                Vector2 p = origin + (float)t * direction;
                if (p.y >= bottom - EPSILON && p.y <= top + EPSILON && t < bestT)
                {
                    bestT = t;
                    intersection.side = Side.Left;
                    intersection.point = p;
                }
            }
        }

        // Top / Bottom
        if (Mathf.Abs(direction.y) > 0f)
        {
            double t = (top - origin.y) / direction.y;
            if (t > EPSILON)
            {
                Vector2 p = origin + (float)t * direction;
                if (p.x >= left - EPSILON && p.x <= right + EPSILON && t < bestT)
                {
                    bestT = t;
                    intersection.side = Side.Top;
                    intersection.point = p;
                }
            }
            t = (bottom - origin.y) / direction.y;
            if (t > EPSILON)
            {
                Vector2 p = origin + (float)t * direction;
                if (p.x >= left - EPSILON && p.x <= right + EPSILON && t < bestT)
                {
                    bestT = t;
                    intersection.side = Side.Bottom;
                    intersection.point = p;
                }
            }
        }

        return intersection;
    }

    public int GetIntersections(Vector2 origin, Vector2 destination, Intersection[] intersections)
    {
        if (intersections == null || intersections.Length == 0) return 0;
        
        //if intersection is a corner, both intersections are equal
        Vector2 direction = destination - origin;
        double[] t = new double[2];
        int i = 0;

        //left
        if (origin.x < left - EPSILON || destination.x < left - EPSILON)
        {
            t[i] = (left - origin.x) / direction.x;
            if (t[i] > EPSILON && t[i] < 1.0 - EPSILON)
            {
                intersections[i].side = Side.Left;
                intersections[i].point = origin + (float)t[i] * direction;
                if (intersections[i].point.y >= bottom - EPSILON && intersections[i].point.y <= top + EPSILON)
                {
                    i++;
                }
            }
        }
        //right
        if (origin.x > right - EPSILON || destination.x > right - EPSILON)
        {
            t[i] = (right - origin.x) / direction.x;
            if (t[i] > EPSILON && t[i] < 1.0 - EPSILON)
            {
                intersections[i].side = Side.Right;
                intersections[i].point = origin + (float)t[i] * direction;
                if (intersections[i].point.y >= bottom - EPSILON && intersections[i].point.y <= top + EPSILON)
                {
                    i++;
                }
            }
        }
        //bottom
        if (origin.y < bottom - EPSILON || destination.y < bottom - EPSILON)
        {
            t[i] = (bottom - origin.y) / direction.y;
            if (i < 2 && t[i] > EPSILON && t[i] < 1.0 - EPSILON)
            {
                intersections[i].side = Side.Bottom;
                intersections[i].point = origin + (float)t[i] * direction;
                if (intersections[i].point.x >= left - EPSILON && intersections[i].point.x <= right + EPSILON)
                {
                    i++;
                }
            }
        }
        //top
        if (origin.y > top - EPSILON || destination.y > top - EPSILON)
        {
            t[i] = (top - origin.y) / direction.y;
            if (i < 2 && t[i] > EPSILON && t[i] < 1.0 - EPSILON)
            {
                intersections[i].side = Side.Top;
                intersections[i].point = origin + (float)t[i] * direction;
                if (intersections[i].point.x >= left - EPSILON && intersections[i].point.x <= right + EPSILON)
                {
                    i++;
                }
            }
        }
        //sort the intersections from nearest to furthest
        if (i == 2 && t[0] > t[1])
        {
            (intersections[1], intersections[0]) = (intersections[0], intersections[1]);
            // Intersection temp = intersections[0];
            // intersections[0] = intersections[1];
            // intersections[1] = temp;
            //tuple swap, IDE
        }
        return i;
    }
}