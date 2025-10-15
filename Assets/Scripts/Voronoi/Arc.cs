using UnityEngine;

public class Arc
{
    public enum Color { RED, BLACK };

    //hierarchy
    public Arc parent;
    public Arc left;
    public Arc right;

    //Diagram
    public VoronoiDiagram.Site site;
    public VoronoiDiagram.HalfEdge leftHalfEdge;
    public VoronoiDiagram.HalfEdge rightHalfEdge;
    public Event circleEvent;

    //optimizations
    public Arc prev;
    public Arc next;

    //only for balancing
    public Color color;

    public Arc() {}
}