using System.Collections.Generic;
using UnityEngine;

public class VoronoiDiagram
{
    public class Site
    {
        public int Index;
        public Vector2 Point;
        public Face Face;
    }

    public class Face
    {
        public Site Site;
        public HalfEdge OuterComponent;
    }

    public class Vertex
    {
        public Vector2 Point;
        internal LinkedListNode<Vertex> Node;
    }

    public class HalfEdge
    {
        public Vertex Origin;
        public Vertex Destination;
        public HalfEdge Twin;
        public HalfEdge Prev;
        public HalfEdge Next;
        public Face IncidentFace;
        internal LinkedListNode<HalfEdge> Node;
    }

    private readonly List<Site> _sites = new();
    private readonly List<Face> _faces = new();
    private readonly LinkedList<Vertex> _vertices = new();
    private readonly LinkedList<HalfEdge> _halfEdges = new();

    public VoronoiDiagram(List<Vector2> points)
    {
        _sites.Capacity = points.Count;
        _faces.Capacity = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            Site site = new Site { Index = i, Point = points[i], Face = null };
            _sites.Add(site);

            Face face = new Face { Site = site, OuterComponent = null };
            _faces.Add(face);

            site.Face = face;
        }
    }

    public Site GetSite(int i)
    {
        return _sites[i];
    }
    public int GetNbSites()
    {
        return _sites.Count;
    }
    public Face GetFace(int i)
    {
        return _faces[i];
    }

    public List<Face> GetAllFaces()
    {
        return _faces;
    }

    public IEnumerable<Vertex> GetVertices()
    {
        return _vertices;
    }
    public IEnumerable<HalfEdge> GetHalfEdges()
    {
        return _halfEdges;
    }

    public bool Intersect(Box box)
    {
        bool error = false;

        HashSet<HalfEdge> processedHalfEdges = new();
        HashSet<Vertex> verticesToRemove = new();

        foreach (var site in _sites)
        {
            if (site.Face == null || site.Face.OuterComponent == null) continue;

            var halfEdge = site.Face.OuterComponent;

            if (halfEdge == null) continue;

            bool inside = box.Contains(halfEdge.Origin.Point);
            bool outerComponentDirty = !inside;

            HalfEdge incomingHalfEdge = null; // first edge entering box
            HalfEdge outgoingHalfEdge = null; // last edge leaving box
            Box.Side incomingSide = default, outgoingSide = default; //TODO: adjust here for unity

            HalfEdge start = halfEdge;
            do
            {
                // try to complete missing endpoints from the twin, if available
                if ((halfEdge.Origin == null || halfEdge.Destination == null) && halfEdge.Twin != null)
                {
                    if (halfEdge.Origin == null && halfEdge.Twin.Destination != null)
                        halfEdge.Origin = halfEdge.Twin.Destination;

                    if (halfEdge.Destination == null && halfEdge.Twin.Origin != null)
                        halfEdge.Destination = halfEdge.Twin.Origin;
                }

                // if still missing an endpoint, skip this half-edge
                if (halfEdge.Origin == null || halfEdge.Destination == null)
                {
                    // move on to the next edge in the face; if none, bail 
                    halfEdge = halfEdge.Next;
                    continue;
                }

                var intersections = new Box.Intersection[4];
                int nbIntersections = box.GetIntersections(
                    halfEdge.Origin.Point, halfEdge.Destination.Point, intersections);

                bool nextInside = box.Contains(halfEdge.Destination.Point);
                var nextHalfEdge = halfEdge.Next;

                // Case 1: both endpoints outside
                if (!inside && !nextInside)
                {
                    if (nbIntersections == 0)
                    {
                        verticesToRemove.Add(halfEdge.Origin);
                        RemoveHalfEdge(halfEdge);
                    }
                    else if (nbIntersections == 2)
                    {
                        verticesToRemove.Add(halfEdge.Origin);

                        if (processedHalfEdges.Contains(halfEdge.Twin))
                        {
                            halfEdge.Origin = halfEdge.Twin.Destination;
                            halfEdge.Destination = halfEdge.Twin.Origin;
                        }
                        else
                        {
                            halfEdge.Origin = CreateVertex(intersections[0].point);
                            halfEdge.Destination = CreateVertex(intersections[1].point);
                        }

                        if (outgoingHalfEdge != null)
                            Link(box, outgoingHalfEdge, outgoingSide, halfEdge, intersections[0].side);

                        if (incomingHalfEdge == null)
                        {
                            incomingHalfEdge = halfEdge;
                            incomingSide = intersections[0].side;
                        }

                        outgoingHalfEdge = halfEdge;
                        outgoingSide = intersections[1].side;
                        processedHalfEdges.Add(halfEdge);
                    }
                    else error = true;
                }
                // Case 2: edge goes from inside -> outside
                else if (inside && !nextInside)
                {
                    if (nbIntersections == 1)
                    {
                        if (processedHalfEdges.Contains(halfEdge.Twin))
                            halfEdge.Destination = halfEdge.Twin.Origin;
                        else
                            halfEdge.Destination = CreateVertex(intersections[0].point);

                        outgoingHalfEdge = halfEdge;
                        outgoingSide = intersections[0].side;
                        processedHalfEdges.Add(halfEdge);
                    }
                    else error = true;
                }
                // Case 3: edge goes from outside -> inside
                else if (!inside && nextInside)
                {
                    if (nbIntersections == 1)
                    {
                        verticesToRemove.Add(halfEdge.Origin);

                        if (processedHalfEdges.Contains(halfEdge.Twin))
                            halfEdge.Origin = halfEdge.Twin.Destination;
                        else
                            halfEdge.Origin = CreateVertex(intersections[0].point);

                        if (outgoingHalfEdge != null)
                            Link(box, outgoingHalfEdge, outgoingSide, halfEdge, intersections[0].side);

                        if (incomingHalfEdge == null)
                        {
                            incomingHalfEdge = halfEdge;
                            incomingSide = intersections[0].side;
                        }

                        processedHalfEdges.Add(halfEdge);
                    }
                    else error = true;
                }

                halfEdge = nextHalfEdge;
                inside = nextInside;

            } while (halfEdge != null && halfEdge != start);

            // Link the last and first half edges inside the box
            if (outerComponentDirty && incomingHalfEdge != null)
                Link(box, outgoingHalfEdge, outgoingSide, incomingHalfEdge, incomingSide);

            // Update outer component if changed
            if (outerComponentDirty)
                site.Face.OuterComponent = incomingHalfEdge;
        }

        // Remove orphaned vertices
        foreach (var v in verticesToRemove)
            RemoveVertex(v);

        return !error;
    }

    public Vertex CreateVertex(Vector2 point)
    {
        Vertex v = new Vertex { Point = point };
        v.Node = _vertices.AddLast(v);
        return v;
    }

    public Vertex CreateCorner(Box box, Box.Side side)
    {
        float left = (float)box.left;
        float right = (float)box.right;
        float top = (float)box.top;
        float bottom = (float)box.bottom;
        return side switch
        {
            Box.Side.Left => CreateVertex(new Vector2(left, top)),
            Box.Side.Bottom => CreateVertex(new Vector2(left, bottom)),
            Box.Side.Right => CreateVertex(new Vector2(right, bottom)),
            Box.Side.Top => CreateVertex(new Vector2(right, top)),
            _ => null
        };
    }

    public HalfEdge CreateHalfEdge(Face face)
    {
        var he = new HalfEdge { IncidentFace = face };
        he.Node = _halfEdges.AddLast(he);

        if (face.OuterComponent == null)
            face.OuterComponent = he;

        return he;
    }

    public void Link(Box box, HalfEdge start, Box.Side startSide, HalfEdge end, Box.Side endSide)
    {
        if (start == null || end == null) return;
        if (start.Destination == null || end.Origin == null) return;

        var halfEdge = start;
        int side = (int)startSide;

        while (side != (int)endSide)
        {
            side = (side + 1) % 4;
            var next = CreateHalfEdge(start.IncidentFace);
            halfEdge.Next = next;
            next.Prev = halfEdge;

            next.Origin = halfEdge.Destination;
            next.Destination = CreateCorner(box, (Box.Side)side);

            halfEdge = next;
        }

        var finalHe = CreateHalfEdge(start.IncidentFace);
        halfEdge.Next = finalHe;
        finalHe.Prev = halfEdge;

        end.Prev = finalHe;
        finalHe.Next = end;

        finalHe.Origin = halfEdge.Destination;
        finalHe.Destination = end.Origin;
    }

    public void RemoveVertex(Vertex v)
    {
        if (v == null) return;
        var node = v.Node;

        if (node == null) return;

        if (node.List != _vertices) { v.Node = null; return; }

        _vertices.Remove(node);
        v.Node = null;
    }

    public void RemoveHalfEdge(HalfEdge he)
    {
        if (he == null) return;

        var node = he.Node;

        if (node == null) return;
        if (node.List != _halfEdges)
        {
            he.Node = null;
            return;
        }

        _halfEdges.Remove(node);
        he.Node = null;

        if (he.Twin != null) he.Twin.Twin = null;
        he.Next = he.Prev = null;
        he.Origin = he.Destination = null;
        he.IncidentFace = null;
    }
}