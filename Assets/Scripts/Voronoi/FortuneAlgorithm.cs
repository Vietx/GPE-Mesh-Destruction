using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class FortuneAlgorithm
{
    public FortuneAlgorithm(List<Vector2> points)
    {
        _diagram = new VoronoiDiagram(new List<Vector2>(points));
        _beachline = new Beachline();
    }

    public void Construct()
    {
        //init event queue
        for (int i = 0; i < _diagram.GetNbSites(); i++)
            _eventQueue.Push(new Event(_diagram.GetSite(i)));

        while (!_eventQueue.IsEmpty())
        {
            Event circleEvent = _eventQueue.Pop();
            _beachlineY = circleEvent.y;

            if (circleEvent.type == Event.Type.SITE)
                HandleSiteEvent(circleEvent);
            else
                HandleCircleEvent(circleEvent);
        }
    }

    public VoronoiDiagram GetDiagram()
    {
        return _diagram;
    }

    private VoronoiDiagram _diagram;
    private Beachline _beachline;
    PriorityQueue<Event> _eventQueue = new();
    private double _beachlineY;

    // Algorithm
    void HandleSiteEvent(Event circleEvent)
    {
        VoronoiDiagram.Site site = circleEvent.site;
        // check if beachline is empty
        if (_beachline.IsEmpty())
        {
            _beachline.SetRoot(_beachline.CreateArc(site));
            return;
        }

        // look for the arc above the site
        Arc arcToBreak = _beachline.LocateArcAbove(site.Point, _beachlineY);
        DeleteEvent(arcToBreak);

        // replace this arc by the new arcs
        Arc middleArc = BreakArc(arcToBreak, site);
        Arc leftArc = middleArc.prev;
        Arc rightArc = middleArc.next;

        // add an edge in the diagram
        AddEdge(leftArc, middleArc);
        middleArc.rightHalfEdge = middleArc.leftHalfEdge;
        rightArc.leftHalfEdge = middleArc.leftHalfEdge;

        // check circle events
        //left triplet
        if (!_beachline.IsNil(leftArc.prev))
            AddEvent(leftArc.prev, leftArc, middleArc);
        //right triplet
        if (!_beachline.IsNil(rightArc.next))
            AddEvent(middleArc, rightArc, rightArc.next);
    }

    void HandleCircleEvent(Event circleEvent)
    {
        Vector2 point = circleEvent.point;
        Arc arc = circleEvent.Arc;
        // add vertex
        VoronoiDiagram.Vertex vertex = _diagram.CreateVertex(point);
        // delete all events with this arc
        Arc leftArc = arc.prev;
        Arc rightArc = arc.next;
        DeleteEvent(leftArc);
        DeleteEvent(rightArc);
        // update the beacline and the diagram
        RemoveArc(arc, vertex);
        // add new circle events
        //left triplet
        if (!_beachline.IsNil(leftArc.prev))
            AddEvent(leftArc.prev, leftArc, rightArc);
        //right triplet
        if (!_beachline.IsNil(rightArc.next))
            AddEvent(leftArc, rightArc, rightArc.next);
    }

    // Arcs
    Arc BreakArc(Arc arc, VoronoiDiagram.Site site)
    {
        // create new subtree
        Arc middleArc = _beachline.CreateArc(site);
        Arc leftArc = _beachline.CreateArc(arc.site);
        leftArc.leftHalfEdge = arc.leftHalfEdge;
        Arc rightArc = _beachline.CreateArc(arc.site);
        rightArc.rightHalfEdge = arc.rightHalfEdge;

        // insert teh subtree in the beachline
        _beachline.Replace(arc, middleArc);
        _beachline.InsertBefore(middleArc, leftArc);
        _beachline.InsertAfter(middleArc, rightArc);

        // return middle arc
        return middleArc;
    }

    void RemoveArc(Arc arc, VoronoiDiagram.Vertex vertex)
    {
        // end edges 
        SetDestination(arc.prev, arc, vertex);
        SetDestination(arc, arc.next, vertex);

        // join edges of the middle arc
        arc.leftHalfEdge.Next = arc.rightHalfEdge;
        arc.rightHalfEdge.Prev = arc.leftHalfEdge;

        // update beachline
        _beachline.Remove(arc);

        // create new edge
        VoronoiDiagram.HalfEdge prevHalfEdge = arc.prev.rightHalfEdge;
        VoronoiDiagram.HalfEdge nextHalfEdge = arc.next.leftHalfEdge;
        AddEdge(arc.prev, arc.next);
        SetOrigin(arc.prev, arc.next, vertex);
        SetPrevHalfEdge(arc.prev.rightHalfEdge, prevHalfEdge);
        SetPrevHalfEdge(nextHalfEdge, arc.next.leftHalfEdge);
    }

    // Breakpoint
    bool IsMovingRight(Arc left, Arc right)
    {
        return left.site.Point.y < right.site.Point.y;
    }
    double GetInitialX(Arc left, Arc right, bool movingRight)
    {
        return movingRight ? left.site.Point.x : right.site.Point.x;
    }

    // Edges
    void AddEdge(Arc left, Arc right)
    {
        // create two new half edges
        left.rightHalfEdge = _diagram.CreateHalfEdge(left.site.Face);
        right.leftHalfEdge = _diagram.CreateHalfEdge(right.site.Face);
        // set the two half edge twins
        left.rightHalfEdge.Twin = right.leftHalfEdge;
        right.leftHalfEdge.Twin = left.rightHalfEdge;
    }

    void SetOrigin(Arc left, Arc right, VoronoiDiagram.Vertex vertex)
    {
        left.rightHalfEdge.Destination = vertex;
        right.leftHalfEdge.Origin = vertex;
    }

    void SetDestination(Arc left, Arc right, VoronoiDiagram.Vertex vertex)
    {
        left.rightHalfEdge.Origin = vertex;
        right.leftHalfEdge.Destination = vertex;
    }

    void SetPrevHalfEdge(VoronoiDiagram.HalfEdge prev, VoronoiDiagram.HalfEdge next)
    {
        prev.Next = next;
        next.Prev = prev;
    }

    // Events
    void AddEvent(Arc left, Arc middle, Arc right)
    {
        Vector2 convergencePoint = ComputeConvergencePoint(left.site.Point, middle.site.Point, right.site.Point, out double y);
        bool isBelow = y <= _beachlineY;
        bool leftBreakpointMovingRight = IsMovingRight(left, middle);
        bool rightBreakpointMovingRight = IsMovingRight(middle, right);
        double leftInitialX = GetInitialX(left, middle, leftBreakpointMovingRight);
        double rightInitialX = GetInitialX(middle, right, rightBreakpointMovingRight);
        bool isValid =
            ((leftBreakpointMovingRight && leftInitialX < convergencePoint.x) ||
            (!leftBreakpointMovingRight && leftInitialX > convergencePoint.x)) &&
            ((rightBreakpointMovingRight && rightInitialX < convergencePoint.x) ||
            (!rightBreakpointMovingRight && rightInitialX > convergencePoint.x));
        if (isValid && isBelow)
        {
            Event circleEvent = new Event(y, convergencePoint, middle);
            middle.circleEvent = circleEvent;
            _eventQueue.Push(circleEvent);
        }
    }

    void DeleteEvent(Arc arc)
    {
        if (arc.circleEvent != null)
        {
            _eventQueue.RemoveAt(arc.circleEvent.HeapIndex);
            arc.circleEvent = null;
        }
    }

    Vector2 ComputeConvergencePoint(Vector2 point1, Vector2 point2, Vector2 point3, out double y)
    {
        Vector2 v1 = GetOrthogonal(point1 - point2);
        Vector2 v2 = GetOrthogonal(point2 - point3);
        Vector2 delta = 0.5f * (point3 - point1);
        float t = GetDet(delta, v2) / GetDet(v1, v2);
        Vector2 center = 0.5f * (point1 + point2) + t * v1;
        float r = Vector2.Distance(center, point1);
        y = center.y - r;
        return center;
    }

    private static Vector2 GetOrthogonal(Vector2 v)
    {
        return new Vector2(-v.y, v.x);
    }

    private static float GetDet(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    // Bounding
    public class LinkedVertex
    {
        public VoronoiDiagram.HalfEdge prevHalfEdge;
        public VoronoiDiagram.Vertex Vertex;
        public VoronoiDiagram.HalfEdge nextHalfEdge;
    }

    public bool Bound(Box box)
    {
        // make sure the bounding box contains all vertices
        foreach (var vertex in _diagram.GetVertices())
        {
            box.left = Mathf.Min((float)box.left, vertex.Point.x);
            box.right = Mathf.Max((float)box.right, vertex.Point.x);
            box.bottom = Mathf.Min((float)box.bottom, vertex.Point.y);
            box.top = Mathf.Max((float)box.top, vertex.Point.y);
        }

        // retrieve all non bounded half edges from beachline
        var linkedVertices = new LinkedList<LinkedVertex>();
        var vertices = new Dictionary<int, LinkedVertex[]>();
        if (!_beachline.IsEmpty())
        {
            Arc leftArc = _beachline.GetLeftMostArc();
            Arc rightArc = leftArc.next;

            while (!_beachline.IsNil(rightArc))
            {
                // bound the edge
                Vector2 direction = GetOrthogonal(leftArc.site.Point - rightArc.site.Point);
                Vector2 origin = (leftArc.site.Point + rightArc.site.Point) * 0.5f;

                //linebox intersection
                Box.Intersection intersection = box.GetFirstIntersection(origin, direction);

                // create a new vertex and end the half edges
                VoronoiDiagram.Vertex vertex = _diagram.CreateVertex(intersection.point);

                // ensure half-edges exist between these adjacent arcs
                if (leftArc.rightHalfEdge == null || rightArc.leftHalfEdge == null)
                {
                    AddEdge(leftArc, rightArc);
                }
                
                SetDestination(leftArc, rightArc, vertex);

                // init pointers
                if (!vertices.ContainsKey(leftArc.site.Index))
                {
                    vertices[leftArc.site.Index] = new LinkedVertex[8];
                }

                if (!vertices.ContainsKey(rightArc.site.Index))
                {
                    vertices[rightArc.site.Index] = new LinkedVertex[8];
                }

                // store the vertex on the boundaries
                linkedVertices.AddLast(new LinkedVertex
                {
                    prevHalfEdge = null,
                    Vertex = vertex,
                    nextHalfEdge = leftArc.rightHalfEdge
                });

                vertices[leftArc.site.Index][2 * (int)intersection.side + 1] = linkedVertices.Last.Value;

                linkedVertices.AddLast(new LinkedVertex
                {
                    prevHalfEdge = rightArc.leftHalfEdge,
                    Vertex = vertex,
                    nextHalfEdge = null
                });

                vertices[rightArc.site.Index][2 * (int)intersection.side] = linkedVertices.Last.Value;

                // next edge
                leftArc = rightArc;
                rightArc = rightArc.next;
            }
        }
        // add corners
        foreach (var kv in vertices)
        {
            var cellVertices = kv.Value;
            for (int i = 0; i < 5; i++)
            {
                int side = i % 4;
                int nextSide = (side + 1) % 4;

                // add first corner
                if (cellVertices[2 * side] == null && cellVertices[2 * side + 1] != null)
                {
                    int prevSide = (side + 3) % 4;
                    VoronoiDiagram.Vertex corner = _diagram.CreateCorner(box, (Box.Side)side);
                    linkedVertices.AddLast(new LinkedVertex
                    {
                        prevHalfEdge = null,
                        Vertex = corner,
                        nextHalfEdge = null
                    });
                    cellVertices[2 * prevSide + 1] = linkedVertices.Last.Value;
                    cellVertices[2 * side] = linkedVertices.Last.Value;
                }

                // add second corner
                else if (cellVertices[2 * side] != null && cellVertices[2 * side + 1] == null)
                {
                    VoronoiDiagram.Vertex corner = _diagram.CreateCorner(box, (Box.Side)nextSide);
                    linkedVertices.AddLast(new LinkedVertex
                    {
                        prevHalfEdge = null,
                        Vertex = corner,
                        nextHalfEdge = null
                    });
                    cellVertices[2 * side + 1] = linkedVertices.Last.Value;
                    cellVertices[2 * nextSide] = linkedVertices.Last.Value;
                }
            }
        }

        // join half edges
        foreach (var kv in vertices)
        {
            int i = kv.Key;
            var cellVertices = kv.Value;
            for (int side = 0; side < 4; side++)
            {
                if (cellVertices[2 * side] != null)
                {
                    // link vertices
                    VoronoiDiagram.HalfEdge halfEdge = _diagram.CreateHalfEdge(_diagram.GetFace(i));
                    halfEdge.Origin = cellVertices[2 * side].Vertex;
                    halfEdge.Destination = cellVertices[2 * side + 1].Vertex;
                    cellVertices[2 * side].nextHalfEdge = halfEdge;
                    halfEdge.Prev = cellVertices[2 * side].prevHalfEdge;

                    if (cellVertices[2 * side].prevHalfEdge != null)
                    {
                        cellVertices[2 * side].prevHalfEdge.Next = halfEdge;
                    }

                    cellVertices[2 * side + 1].prevHalfEdge = halfEdge;
                    halfEdge.Next = cellVertices[2 * side + 1].nextHalfEdge;

                    if (cellVertices[2 * side + 1].nextHalfEdge != null)
                    {
                        cellVertices[2 * side + 1].nextHalfEdge.Prev = halfEdge;
                    }
                }
            }
        }
        return true; //TODO detect error <--- actual comment from c++ code.
    }
}