using UnityEngine;

public class Beachline
{
    private Arc _nil;
    private Arc _root;

    public Beachline()
    {
        _nil = new Arc
        {
            color  = Arc.Color.BLACK,
            parent = null,
            left   = null,
            right  = null,
            prev   = null,
            next   = null
        };
        
        _nil.parent = _nil.left = _nil.right = _nil;

        _root = _nil;
    }

    public Arc CreateArc(VoronoiDiagram.Site site)
    {
        var a = new Arc
        {
            site  = site,
            color = Arc.Color.RED,

            parent = _nil,
            left   = _nil,
            right  = _nil,

            prev = null,
            next = null,

            leftHalfEdge  = null,
            rightHalfEdge = null,
            circleEvent   = null
        };
        return a;
    }

    public bool IsEmpty()
    {
        return IsNil(_root);
    }

    public bool IsNil(Arc x)
    {
        return x == null || ReferenceEquals(x, _nil);
    }

    public void SetRoot(Arc x)
    {
        _root = x;
        _root.color = Arc.Color.BLACK;
    }

    public Arc GetLeftMostArc()
    {
        Arc x = _root;
        while (!IsNil(x.left))
            x = x.left;
        return x;
    }

    public Arc LocateArcAbove(Vector2 point, double l)
    {
        if (IsEmpty()) return _nil;

        Arc node = _root;
        bool found = false;

        while (!found)
        {
            double breakpointLeft = double.NegativeInfinity;
            double breakpointRight = double.PositiveInfinity;

            if (!IsNil(node.prev))
                breakpointLeft = ComputeBreakpoint(node.prev.site.Point, node.site.Point, l);

            if (!IsNil(node.next))
                breakpointRight = ComputeBreakpoint(node.site.Point, node.next.site.Point, l);

            if (point.x < breakpointLeft)
                node = node.left;
            else if (point.x > breakpointRight)
                node = node.right;
            else
                found = true;
        }

        return node;
    }

    public void InsertBefore(Arc x, Arc y)
    {
        //find the right place
        if (IsNil(x.left))
        {
            x.left = y;
            y.parent = x;
        }
        else
        {
            x.prev.right = y;
            y.parent = x.prev;
        }

        //set the pointers
        y.prev = x.prev;

        if (!IsNil(y.prev))
            y.prev.next = y;

        y.next = x;
        x.prev = y;
        //balance the tree
        InsertFixup(y);
    }

    public void InsertAfter(Arc x, Arc y)
    {
        //find the right place
        if (IsNil(x.right))
        {
            x.right = y;
            y.parent = x;
        }
        else
        {
            x.next.left = y;
            y.parent = x.next;
        }

        //set the pointers
        y.next = x.next;

        if (!IsNil(y.next))
            y.next.prev = y;

        y.prev = x;
        x.next = y;
        //balance the tree
        InsertFixup(y);
    }

    public void Replace(Arc x, Arc y)
    {
        Transplant(x, y);
        y.left = x.left;
        y.right = x.right;

        if (!IsNil(y.left))
            y.left.parent = y;

        if (!IsNil(y.right))
            y.right.parent = y;

        y.prev = x.prev;
        y.next = x.next;

        if (!IsNil(y.prev))
            y.prev.next = y;
        if (!IsNil(y.next))
            y.next.prev = y;
        
        y.color = x.color;
    }

    public void Remove(Arc z)
    {
        Arc y = z;
        Arc.Color yOriginalColor = y.color;
        Arc x;

        if (IsNil(z.left))
        {
            x = z.right;
            Transplant(z, z.right);
        }
        else if (IsNil(z.right))
        {
            x = z.left;
            Transplant(z, z.left);
        }
        else
        {
            y = Minimum(z.right);
            yOriginalColor = y.color;
            x = y.right;

            if (y.parent == z)
                x.parent = y;
            else
            {
                Transplant(y, y.right);
                y.right = z.right;
                y.right.parent = y;
            }

            Transplant(z, y);
            y.left = z.left;
            y.left.parent = y;
            y.color = z.color;
        }

        if (yOriginalColor == Arc.Color.BLACK)
            RemoveFixup(x);

        //clear pointers
        if (!IsNil(z.prev))
            z.prev.next = z.next;
        if (!IsNil(z.next))
            z.next.prev = z.prev;
    }

    Arc Minimum(Arc x)
    {
        while (!IsNil(x.left))
            x = x.left;
        return x;
    }

    void Transplant(Arc u, Arc v)
    {
        if (IsNil(u.parent))
            _root = v;
        else if (u == u.parent.left)
            u.parent.left = v;
        else
            u.parent.right = v;

        v.parent = u.parent;
    }

    void InsertFixup(Arc z)
    {
        while (z.parent.color == Arc.Color.RED)
        {
            if (z.parent == z.parent.parent.left)
            {
                Arc y = z.parent.parent.right;
                if (y.color == Arc.Color.RED)
                {
                    z.parent.color = Arc.Color.BLACK;
                    y.color = Arc.Color.BLACK;
                    z.parent.parent.color = Arc.Color.RED;
                    z = z.parent.parent;
                }
                else
                {
                    if (z == z.parent.right)
                    {
                        z = z.parent;
                        LeftRotate(z);
                    }
                    z.parent.color = Arc.Color.BLACK;
                    z.parent.parent.color = Arc.Color.RED;
                    RightRotate(z.parent.parent);
                }
            }
            else
            {
                Arc y = z.parent.parent.left;
                if (y.color == Arc.Color.RED)
                {
                    z.parent.color = Arc.Color.BLACK;
                    y.color = Arc.Color.BLACK;
                    z.parent.parent.color = Arc.Color.RED;
                    z = z.parent.parent;
                }
                else
                {
                    if (z == z.parent.left)
                    {
                        z = z.parent;
                        RightRotate(z);
                    }
                    z.parent.color = Arc.Color.BLACK;
                    z.parent.parent.color = Arc.Color.RED;
                    LeftRotate(z.parent.parent);
                }
            }
        }
        _root.color = Arc.Color.BLACK;
    }

    void RemoveFixup(Arc x)
    {
        while (x != _root && x.color == Arc.Color.BLACK)
        {
            Arc w;
            if (x == x.parent.left)
            {
                w = x.parent.right;
                if (w.color == Arc.Color.RED)
                {
                    w.color = Arc.Color.BLACK;
                    x.parent.color = Arc.Color.RED;
                    LeftRotate(x.parent);
                    w = x.parent.right;
                }

                if (w.left.color == Arc.Color.BLACK && w.right.color == Arc.Color.BLACK)
                {
                    w.color = Arc.Color.RED;
                    x = x.parent;
                }
                else
                {
                    if (w.right.color == Arc.Color.BLACK)
                    {
                        w.left.color = Arc.Color.BLACK;
                        w.color = Arc.Color.RED;
                        RightRotate(w);
                        w = x.parent.right;
                    }

                    w.color = x.parent.color;
                    x.parent.color = Arc.Color.BLACK;
                    w.right.color = Arc.Color.BLACK;
                    LeftRotate(x.parent);
                    x = _root;
                }
            }
            else
            {
                w = x.parent.left;
                if (w.color == Arc.Color.RED)
                {
                    w.color = Arc.Color.BLACK;
                    x.parent.color = Arc.Color.RED;
                    RightRotate(x.parent);
                    w = x.parent.left;
                }

                if (w.right.color == Arc.Color.BLACK && w.left.color == Arc.Color.BLACK)
                {
                    w.color = Arc.Color.RED;
                    x = x.parent;
                }
                else
                {
                    if (w.left.color == Arc.Color.BLACK)
                    {
                        w.right.color = Arc.Color.BLACK;
                        w.color = Arc.Color.RED;
                        LeftRotate(w);
                        w = x.parent.left;
                    }

                    w.color = x.parent.color;
                    x.parent.color = Arc.Color.BLACK;
                    w.left.color = Arc.Color.BLACK;
                    RightRotate(x.parent);
                    x = _root;
                }
            }
        }
        x.color = Arc.Color.BLACK;
    }

    void LeftRotate(Arc x)
    {
        Arc y = x.right;
        x.right = y.left;

        if (!IsNil(y.left))
            y.left.parent = x;

        y.parent = x.parent;

        if (IsNil(x.parent))
            _root = y;
        else if (x == x.parent.left)
            x.parent.left = y;
        else
            x.parent.right = y;

        y.left = x;
        x.parent = y;
    }

    void RightRotate(Arc y)
    {
        Arc x = y.left;
        y.left = x.right;

        if (!IsNil(x.right))
            x.right.parent = y;

        x.parent = y.parent;

        if (IsNil(y.parent))
            _root = x;
        else if (y == y.parent.left)
            y.parent.left = x;
        else
            y.parent.right = x;

        x.right = y;
        y.parent = x;
    }

    double ComputeBreakpoint(Vector2 point1, Vector2 point2, double l)
    {
        double x1 = point1.x, y1 = point1.y, x2 = point2.x, y2 = point2.y;
        double d1 = 1.0 / (2.0 * (y1 - l));
	    double d2 = 1.0 / (2.0 * (y2 - l));
	    double a = d1 - d2;
	    double b = 2.0 * (x2 * d2 - x1 * d1);
	    double c = (y1 * y1 + x1 * x1 - l * l) * d1 - (y2 * y2 + x2 * x2 - l * l) * d2;
	    double delta = b * b - 4.0 * a * c;
        return (-b + Mathf.Sqrt((float)delta)) / (2.0 * a);
    }
}
