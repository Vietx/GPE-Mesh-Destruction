public class HalfEdge
{
    Vertex origin;
    Vertex destination;
    HalfEdge twin;
    Face incindentFace;
    HalfEdge prev;
    HalfEdge next;
}

/*
https://pvigier.github.io/2018/11/18/fortune-algorithm-details.html
DCEL = Doubly connected edge list
You might wonder what is a half-edge. An edge in a Voronoi diagram is shared by two adjacent cells. 
In the DCEL data structure, we split these edges in two half-edges, one for each cell, and they are linked by the twin pointer. 
Moreover, a half-edge has an origin vertex and a destination vertex. The incidentFace field points to the face to which the half-edge belongs to. 
Finally, in DCEL, cells are implemented as a circular doubly linked list of half-edges where adjacent half-edges are linked together. 
Thus the prev and next fields points to the previous and next half-edges in the cell.
*/