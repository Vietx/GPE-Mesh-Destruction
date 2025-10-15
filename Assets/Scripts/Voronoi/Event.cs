using System;
using UnityEngine;

public class Event : IComparable<Event>, IIndexed
{
    public enum Type { SITE, CIRCLE };
    public int HeapIndex { get; set; } = -1;

    public Event(VoronoiDiagram.Site site)
    {
        this.type = Type.SITE;
        this.site = site;
        this.point = site.Point;
        this.index = site.Index;
    }

    public Event(double y, Vector2 point, Arc arc)
    {
        this.type = Type.CIRCLE;
        this.y = y;
        this.point = point;
        this.Arc = arc;
        this.site = arc.site;
    }

    public Type type;
    public double y;
    public int index;
    public VoronoiDiagram.Site site;
    public Vector2 point;
    public Arc Arc;

    public bool Operator(Event other)
    {
        return y < other.y;
    }

    public int CompareTo(Event other)
    {
        if (other == null) return 1;
        return other.y.CompareTo(y);
    }
}