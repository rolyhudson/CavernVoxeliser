using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CavernVoxel
{
    class DiagonalMember
    {
        public List<Point3d> points = new List<Point3d>();
        public List<Point3d> endpoints = new List<Point3d>();
        public Line diagonal;
        public bool needed = true;
        double dnum;
        public DiagonalMember(int dNum,List<Point3d> nodegrid)
        {
            dnum = dNum;
            switch(dnum)
            {
                case 0:
                    endpoints.Add(nodegrid[0]);
                    endpoints.Add(nodegrid[7]);
                    points.Add(nodegrid[1]);
                    points.Add(nodegrid[0]);
                    points.Add(nodegrid[6]);
                    points.Add(nodegrid[7]);
                    break;
                case 1:
                    endpoints.Add(nodegrid[0]);
                    endpoints.Add(nodegrid[10]);
                    points.Add(nodegrid[0]);
                    points.Add(nodegrid[8]);
                    points.Add(nodegrid[10]);
                    points.Add(nodegrid[6]);
                    break;
                case 2:
                    endpoints.Add(nodegrid[2]);
                    endpoints.Add(nodegrid[10]);
                    points.Add(nodegrid[8]);
                    points.Add(nodegrid[2]);
                    points.Add(nodegrid[4]);
                    points.Add(nodegrid[10]);
                    break;
                case 3:
                    endpoints.Add(nodegrid[2]);
                    endpoints.Add(nodegrid[5]);
                    points.Add(nodegrid[2]);
                    points.Add(nodegrid[3]);
                    points.Add(nodegrid[5]);
                    points.Add(nodegrid[4]);
                    break;
                case 4:
                    endpoints.Add(nodegrid[3]);
                    endpoints.Add(nodegrid[11]);
                    points.Add(nodegrid[3]);
                    points.Add(nodegrid[9]);
                    points.Add(nodegrid[11]);
                    points.Add(nodegrid[5]);
                    break;
                case 5:
                    endpoints.Add(nodegrid[1]);
                    endpoints.Add(nodegrid[11]);
                    points.Add(nodegrid[1]);
                    points.Add(nodegrid[9]);
                    points.Add(nodegrid[11]);
                    points.Add(nodegrid[7]);
                    break;
                case 6:
                    endpoints.Add(nodegrid[8]);
                    endpoints.Add(nodegrid[11]);
                    points.Add(nodegrid[8]);
                    points.Add(nodegrid[9]);
                    points.Add(nodegrid[11]);
                    points.Add(nodegrid[10]);
                    break;
                case 7:
                    endpoints.Add(nodegrid[1]);
                    endpoints.Add(nodegrid[8]);
                    points.Add(nodegrid[0]);
                    points.Add(nodegrid[8]);
                    points.Add(nodegrid[9]);
                    points.Add(nodegrid[1]);
                    break;
                case 8:
                    endpoints.Add(nodegrid[3]);
                    endpoints.Add(nodegrid[8]);
                    points.Add(nodegrid[2]);
                    points.Add(nodegrid[3]);
                    points.Add(nodegrid[9]);
                    points.Add(nodegrid[8]);
                    break;
                case 9:
                    endpoints.Add(nodegrid[5]);
                    endpoints.Add(nodegrid[10]);
                    points.Add(nodegrid[4]);
                    points.Add(nodegrid[5]);
                    points.Add(nodegrid[11]);
                    points.Add(nodegrid[10]);
                    break;
                case 10:
                    endpoints.Add(nodegrid[7]);
                    endpoints.Add(nodegrid[10]);
                    points.Add(nodegrid[6]);
                    points.Add(nodegrid[7]);
                    points.Add(nodegrid[11]);
                    points.Add(nodegrid[10]);
                    break;
            }
            diagonal = new Line(endpoints[0], endpoints[1]);
        }
        //public DiagonalMember(int dNum,Brep bound)
        //{
        //    dnum = dNum;
        //    switch (dNum)
        //    {
        //        case 0:
        //            endpoints.Add(bound.Vertices[1].Location);
        //            endpoints.Add(bound.Vertices[6].Location);
        //            points.Add(bound.Vertices[1].Location);
        //            points.Add(bound.Vertices[0].Location);
        //            points.Add(bound.Vertices[6].Location);
        //            points.Add(bound.Vertices[7].Location);
        //            break;
        //        case 1:
        //            endpoints.Add(bound.Vertices[2].Location);
        //            endpoints.Add(bound.Vertices[6].Location);
        //            points.Add(bound.Vertices[0].Location);
        //            points.Add(bound.Vertices[2].Location);
        //            points.Add(bound.Vertices[4].Location);
        //            points.Add(bound.Vertices[6].Location);
        //            break;
        //        case 2:
        //            endpoints.Add(bound.Vertices[2].Location);
        //            endpoints.Add(bound.Vertices[5].Location);
        //            points.Add(bound.Vertices[3].Location);
        //            points.Add(bound.Vertices[2].Location);
        //            points.Add(bound.Vertices[4].Location);
        //            points.Add(bound.Vertices[5].Location);
        //            break;
        //        case 3:
        //            endpoints.Add(bound.Vertices[5].Location);
        //            endpoints.Add(bound.Vertices[1].Location);
        //            points.Add(bound.Vertices[1].Location);
        //            points.Add(bound.Vertices[3].Location);
        //            points.Add(bound.Vertices[5].Location);
        //            points.Add(bound.Vertices[7].Location);
        //            break;
        //        case 4:
        //            endpoints.Add(bound.Vertices[1].Location);
        //            endpoints.Add(bound.Vertices[2].Location);
        //            points.Add(bound.Vertices[1].Location);
        //            points.Add(bound.Vertices[0].Location);
        //            points.Add(bound.Vertices[2].Location);
        //            points.Add(bound.Vertices[3].Location);
        //            break;
        //        case 5:
        //            endpoints.Add(bound.Vertices[5].Location);
        //            endpoints.Add(bound.Vertices[6].Location);
        //            points.Add(bound.Vertices[4].Location);
        //            points.Add(bound.Vertices[5].Location);
        //            points.Add(bound.Vertices[7].Location);
        //            points.Add(bound.Vertices[6].Location);
        //            break;
        //    }
        //    diagonal = new Line(endpoints[0], endpoints[1]);
        //}
        public void trim(Mesh m)
        {
            if (StructuralCell.curveIsInsideMesh(diagonal.ToNurbsCurve(), m))
            {
                needed = false;
            }
            else
            {
                int[] faceIds;
                Point3d[] points = Rhino.Geometry.Intersect.Intersection.MeshLine(m, diagonal, out faceIds);
                if (points.Length > 0)
                {
                    //try find new diagonal
                    newDiagonal(m);
                }
            }
            
        }
        private void newDiagonal(Mesh m)
        {
            
            //find the new set of points afte the intersection
            List<Point3d> newPts = new List<Point3d>();
            //store the indices of the diagonal ends
            List<int> possEnds = new List<int>();
            for (int p = 0; p < points.Count; p++)
            {
                if (!pointIsInsideMesh(m, points[p]))
                {
                    newPts.Add(points[p]);

                }
                
                Line l = new Line();
                if (p == points.Count - 1) l = new Line(points[p], points[0]);
                else l = new Line(points[p], points[p + 1]);
                int[] faceIds;
                Point3d[] ipts = Rhino.Geometry.Intersect.Intersection.MeshLine(m, l, out faceIds);
                
                //use only first intersect
                if(ipts.Length>0)
                {
                    possEnds.Add(p);
                    newPts.Add(ipts[0]);
                }
            }
            //find the possible diagonals
            if (newPts.Count > 3 && possEnds.Count ==2)
            {
                if (newPts.Count == 4)
                {
                    int end = (possEnds[0] + 2) % 4;
                    Line l1 = new Line(newPts[possEnds[0]], newPts[end]);
                    end = (possEnds[1] + 2) % 4;
                    Line l2 = new Line(newPts[possEnds[1]], newPts[end]);
                    if (l1.Length < l2.Length) diagonal = l1;
                    else diagonal = l2;
                }
                if (newPts.Count == 5)
                {
                    int start = posmod(possEnds[0] - 1, 5);
                    int end = posmod(possEnds[1] + 1, 5);
                    diagonal = new Line(newPts[start], newPts[end]);
                }
            }
        }
        int posmod(int x, int m)
        {
            return (x % m + m) % m;
        }
        private bool pointIsInsideMesh(Mesh m, Point3d p)
        {
            //mesh normals towards inside
            MeshPoint mp = m.ClosestMeshPoint(p, 0);
            Vector3d v = p - mp.Point;
            if (Vector3d.VectorAngle(v, m.FaceNormals[mp.FaceIndex]) < Math.PI / 2)
            {
                //inside
                return true;
            }
            return false;
        }
    }
}
