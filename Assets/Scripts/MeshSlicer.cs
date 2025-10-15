using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MeshSlicer
{
    public static Mesh[] SliceMesh(Mesh mesh, Vector3 cutOrigin, Vector3 cutNormal)
    {
        Plane plane = new Plane(cutNormal, cutOrigin);
        MeshContructionHelper positiveMesh = new MeshContructionHelper();
        MeshContructionHelper negativeMesh = new MeshContructionHelper();

        int[] meshTriangles = mesh.triangles;
        List<VertexData> pointsAlongPlane = new List<VertexData>();

        for (int i = 0; i < meshTriangles.Length; i += 3)
        {
            VertexData vertexA = GetVertexData(mesh, plane, meshTriangles[i]);
            VertexData vertexB = GetVertexData(mesh, plane, meshTriangles[i + 1]);
            VertexData vertexC = GetVertexData(mesh, plane, meshTriangles[i + 2]);

            bool isABSameSide = vertexA.Side == vertexB.Side;
            bool isBCSameSide = vertexB.Side == vertexC.Side;
            //checks if plane is on the +/- side of a plane

            if (isABSameSide && isBCSameSide)
            {
                //no intersection
                MeshContructionHelper helper = vertexA.Side ? positiveMesh : negativeMesh;
                helper.AddMeshSection(vertexA, vertexB, vertexC);
            }
            else
            {
                //else find intersection
                VertexData intersectionD;
                VertexData intersectionE;

                MeshContructionHelper helperA = vertexA.Side ? positiveMesh : negativeMesh;
                MeshContructionHelper helperB = vertexB.Side ? positiveMesh : negativeMesh;
                MeshContructionHelper helperC = vertexC.Side ? positiveMesh : negativeMesh;

                if (isABSameSide)
                {
                    intersectionD = GetIntersectionVertex(vertexA, vertexC, cutOrigin, cutNormal);
                    intersectionE = GetIntersectionVertex(vertexB, vertexC, cutOrigin, cutNormal);

                    helperA.AddMeshSection(vertexA, vertexB, intersectionE);
                    helperA.AddMeshSection(vertexA, intersectionE, intersectionD);
                    helperC.AddMeshSection(intersectionE, vertexC, intersectionD);
                }
                else if (isBCSameSide)
                {
                    intersectionD = GetIntersectionVertex(vertexB, vertexA, cutOrigin, cutNormal);
                    intersectionE = GetIntersectionVertex(vertexC, vertexA, cutOrigin, cutNormal);

                    helperB.AddMeshSection(vertexB, vertexC, intersectionE);
                    helperB.AddMeshSection(vertexB, intersectionE, intersectionD);
                    helperA.AddMeshSection(intersectionE, vertexA, intersectionD);
                }
                else
                {
                    intersectionD = GetIntersectionVertex(vertexA, vertexB, cutOrigin, cutNormal);
                    intersectionE = GetIntersectionVertex(vertexC, vertexB, cutOrigin, cutNormal);

                    helperA.AddMeshSection(vertexA, intersectionE, vertexC);
                    helperA.AddMeshSection(intersectionD, intersectionE, vertexA);
                    helperB.AddMeshSection(vertexB, intersectionE, intersectionD);
                }

                pointsAlongPlane.Add(intersectionD);
                pointsAlongPlane.Add(intersectionE);
            }
        }

        JoinPointsAlongPlane(ref positiveMesh, ref negativeMesh, cutNormal, pointsAlongPlane);

        return new[] {
            positiveMesh.ConstructMesh(),
            negativeMesh.ConstructMesh(),
        };
    }
    
    private static VertexData GetVertexData(Mesh mesh, Plane plane, int index)
    {
        Vector3 position = mesh.vertices[index];
        VertexData vertexData = new VertexData()
        {
            Position = position,
            Side = plane.GetSide(position),
            Uv = mesh.uv[index],
            Normal = mesh.normals[index]
        };
        return vertexData;
    }

    /// <summary>
    /// if the vector is not perpendicular, a intersection exists
    /// </summary>
    public static bool PointsIntersectsAPlane(Vector3 from, Vector3 to, Vector3 planeOrigin, Vector3 normal, out Vector3 result)
    {
        Vector3 translation = to - from;
        float dot = Vector3.Dot(normal, translation);

        if (Mathf.Abs(dot) > Single.Epsilon)
        {
            Vector3 fromOrigin = from - planeOrigin;
            //interpolation factor that tells you how far between from and to, the intersection is
            float fac = -Vector3.Dot(normal, fromOrigin) / dot;
            translation = translation * fac;
            result = from + translation;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private static VertexData GetIntersectionVertex(VertexData vertexA, VertexData vertexB, Vector3 planeOrigin, Vector3 normal)
    {
        PointsIntersectsAPlane(vertexA.Position, vertexB.Position, planeOrigin, normal, out Vector3 result);
        float distanceA = Vector3.Distance(vertexA.Position, result);
        float distanceB = Vector3.Distance(vertexB.Position, result);
        float t = distanceA / (distanceA + distanceB);

        return new VertexData()
        {
            Position = result,
            Normal = normal,
            Uv = VertexUtility.InterpolateUvs(vertexA.Uv, vertexB.Uv, t)
        };
    }
    
    private static void JoinPointsAlongPlane(ref MeshContructionHelper positive, ref MeshContructionHelper negative, Vector3 cutNormal, List<VertexData> pointsAlongPlane)
    {
        VertexData halfway = new VertexData()
        {
            Position = VertexUtility.GetHalfwayPoint(pointsAlongPlane)
        };
        
        for (int i = 0; i <pointsAlongPlane.Count; i += 2)
        {
            VertexData firstVertex = pointsAlongPlane[i];
            VertexData secondVertex =  pointsAlongPlane[i+1];

            Vector3 normal = VertexUtility.ComputeNormal(halfway, secondVertex, firstVertex);
            halfway.Normal = Vector3.forward;

            float dot = Vector3.Dot(normal, cutNormal);
            //we check which side of our plane the calculated normal is
            //and we add new triangle to both construction helpers 

            if(dot > 0)
            {             
                //used if calculated normal aligns with plane normal                           
                positive.AddMeshSection(firstVertex, secondVertex, halfway);
                negative.AddMeshSection(secondVertex, firstVertex,halfway);
            }
            else
            {
                //used if calculated normal is opposite to plane normal
                negative.AddMeshSection(firstVertex, secondVertex, halfway);
                positive.AddMeshSection(secondVertex, firstVertex,halfway);
            }      
        }
    }
}
