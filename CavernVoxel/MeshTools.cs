using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino;

namespace CavernVoxel
{
    class MeshTools
    {
        public static Mesh findIntersection(Mesh meshToSplit, StructuralCell c)
        {
            Mesh extendSplitter = makeCuboid(c.cellPlane, c.xDim,c.yDim, c.zDim);
            Mesh result = splitMeshWithMesh(meshToSplit, extendSplitter);
            int inc = 0;

            while (result == null || isJaggedBorder(result, extendSplitter))
            {
                //make a bigger splitter
                inc -= 1;
                if (inc < -10) break;
                extendSplitter = makeCuboid(c.cellPlane, c.xDim + inc, c.yDim + inc, c.zDim + inc);
                
                result = splitMeshWithMesh(meshToSplit, extendSplitter);
            }
            
            return result;
        }
        private static bool isJaggedBorder(Mesh result,Mesh extendSplitter)
        {
            foreach (MeshFace mf in result.Faces)
            {
                //if its a jagged border the face centroid points should be outside
                Point3d p = new Point3d(0,0,0);
                p.X = (result.Vertices[mf.A].X + result.Vertices[mf.B].X + result.Vertices[mf.C].X + result.Vertices[mf.D].X)/4;
                p.Y = (result.Vertices[mf.A].Y + result.Vertices[mf.B].Y + result.Vertices[mf.C].Y + result.Vertices[mf.D].Y) / 4;
                p.Z = (result.Vertices[mf.A].Z + result.Vertices[mf.B].Z + result.Vertices[mf.C].Z + result.Vertices[mf.D].Z) / 4;
                if (!extendSplitter.IsPointInside(p, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, false))
                {
                    //found a face centroid outside the splitter
                    return true;
                }
            }
            return false;
        }
        public static Mesh splitMeshWithMesh(Mesh meshToSplit, Mesh closedSplitter)
        {
            var splits = meshToSplit.Split(closedSplitter);
            if (splits.Count() > 0)
            {
                //get the samller of the two
                if (splits[0].Vertices.Count > splits[1].Vertices.Count)
                {
                    return splits[1];
                }
                else
                {
                    return splits[0];
                }
            }
            return null;
            
        }
        public static bool curveInBrep(Curve c, Brep b)
        {
            NurbsCurve nc = c.ToNurbsCurve();
            
            foreach(ControlPoint cp in nc.Points)
            {
                if (!b.IsPointInside(cp.Location, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, true)) return false;
            }
            //all points were inside
            return true;
        }
        public static Mesh makeCuboid(Plane pln, double width, double depth, double height)
        {
            Mesh cell = new Mesh();
            Box box = new Box(pln, new Interval(-width / 2, width / 2),new Interval (-depth/2,depth/2), new Interval(-height / 2, height / 2));
            cell = Mesh.CreateFromBox(box, 1, 1, 1);
            return cell;
        }
        public static Mesh makeCuboid(Plane pln, double width, Interval depth, double height)
        {
            //non equal y interval
            Mesh cell = new Mesh();
            Box box = new Box(pln, new Interval(-width / 2, width / 2), depth, new Interval(-height / 2, height / 2));
            cell = Mesh.CreateFromBox(box, 1, 1, 1);
            return cell;
        }
        public static void matchOrientation(Mesh toMatch,ref Mesh toOrient)
        {
            toOrient.FaceNormals.ComputeFaceNormals();
            Point3d av = meshCentroid(toOrient);
            Vector3d v = new Vector3d();
            foreach (Vector3d mf in toOrient.FaceNormals)
            {
                v += mf;
            }
            MeshPoint mp = toMatch.ClosestMeshPoint(av, 0);
            Vector3d outside = toMatch.FaceNormals[mp.FaceIndex];
            if (Vector3d.VectorAngle(outside, v) > Math.PI / 2)
            {
                toOrient.Flip(true,true,true);
            }
        }
        public static Point3d meshCentroid(Mesh m)
        {
            List<Point3d> points = new List<Point3d>();
            foreach (Point3d p in m.Vertices) points.Add(p);
            Point3d centroid = averagePoint(points);
            return centroid;
        }
        public static Point3d averagePoint(List<Point3d> points)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            foreach (Point3d p in points)
            {
                x += p.X;
                y += p.Y;
                z += p.Z;
            }
            Point3d centroid = new Point3d(x / points.Count, y / points.Count, z / points.Count);
            return centroid;
        }
        private static Mesh splitMeshWithPlanes(Mesh toSplit,Mesh splitter)
        {
            Point3d boundCentroid = meshCentroid(splitter);
            List<Mesh> results = new List<Mesh>();
            Mesh trimmedMesh = toSplit;

            for (int i = 0; i < splitter.Faces.Count; i++)
            {
                MeshFace f = splitter.Faces[i];
                Vector3d normal = splitter.FaceNormals[i];
                Point3d origin = averagePoint(new List<Point3d> { splitter.Vertices[f.A], splitter.Vertices[f.B], splitter.Vertices[f.C], splitter.Vertices[f.D] });
                Plane facePlane = new Plane(origin, normal);
                Vector3d toCentre = boundCentroid - origin;
                if (Vector3d.VectorAngle(facePlane.Normal, toCentre) > Math.PI / 2)
                {
                    facePlane.Flip();
                }
                trimmedMesh = split(trimmedMesh, facePlane);
                
            }
            return trimmedMesh;
        }
        public static Box findBBox(Mesh m)
        {
            BoundingBox minBBox = new BoundingBox();
            double minVol = Double.MaxValue;
            double theta = Math.PI / 100;
            Plane minBBoxPln = new Plane();
            double minBBoxRot = 0;
            for (int i = 0; i < 100; i++)
            {
                Plane pln = Plane.WorldXY;
                pln.Rotate(theta * i, Vector3d.ZAxis);
                List<Point3d> pts = new List<Point3d>();
                
                foreach (Point3d p in m.Vertices)
                {
                    Point3d remapped = new Point3d();
                    pln.RemapToPlaneSpace(p, out remapped);

                    pts.Add(remapped);
                }
                
                BoundingBox bBox = new BoundingBox(pts);
                if (bBox.Volume < minVol)
                {
                    minBBoxPln = pln;
                    minVol = bBox.Volume;
                    minBBox = bBox;
                    minBBoxRot = theta * i;
                }

            }
            Interval xInt = new Interval(0, minBBox.Max.X - minBBox.Min.X);
            Interval yInt = new Interval(0, minBBox.Max.Y - minBBox.Min.Y);
            Interval zInt = new Interval(0, minBBox.Max.Z - minBBox.Min.Z);
            
            Transform xform = Transform.PlaneToPlane(Plane.WorldXY, minBBoxPln);
            Point3d origin = minBBox.Min;
            origin.Transform(xform);
            Plane gridPlane = new Plane(origin, Vector3d.ZAxis);
            gridPlane.Rotate(minBBoxRot, Vector3d.ZAxis);
            Box box = new Box(gridPlane, xInt, yInt, zInt);
            return box;
        }
        public static Mesh splitTwoPlanes(Plane p1, Plane p2, Mesh m)
        {
            var m1 = split(m, p1);
            
            var m2 = split(m1, p2);
            
            return m2;
        }
        private static Mesh split(Mesh m, Plane p)
        {
            Mesh splitMesh = new Mesh();

            foreach (MeshFace f in m.Faces)
            {
                var pts = getFacePoints(f, m);
                if (pointsInsidePlane(pts, p))
                {
                    addPointsAndFace(pts, ref splitMesh);
                }
                else
                {
                    faceSplit(pts, p, ref splitMesh);
                }
            }
            splitMesh.Faces.CullDegenerateFaces();
            splitMesh.Faces.ExtractDuplicateFaces();
            splitMesh.Vertices.CullUnused();
            splitMesh.Vertices.CombineIdentical(true, true);
            return splitMesh;
        }
        private static void faceSplit(List<Point3d> pts, Plane pln, ref Mesh meshToAppend)
        {
            List<Point3d> newPts = new List<Point3d>();
            for (int p = 0; p < pts.Count; p++)
            {
                if (pointInsidePlane(pts[p], pln))
                {
                    newPts.Add(pts[p]);

                }
                double t = 0;
                Line l = new Line();
                if (p == pts.Count - 1) l = new Line(pts[p], pts[0]);
                else l = new Line(pts[p], pts[p + 1]);

                Rhino.Geometry.Intersect.Intersection.LinePlane(l, pln, out t);

                if (t >= 0 && t <= 1)
                {
                    newPts.Add(l.PointAt(t));
                }
            }
            if (newPts.Count == 4 || newPts.Count == 3) addPointsAndFace(newPts, ref meshToAppend);
            if (newPts.Count == 5) add5PointsAndFaces(newPts, ref meshToAppend);
        }
        private static void add5PointsAndFaces(List<Point3d> pts, ref Mesh m)
        {
            int vcount = m.Vertices.Count;
            m.Vertices.AddVertices(pts);
            m.Faces.AddFace(vcount, vcount + 1, vcount + 2, vcount + 3);
            m.Faces.AddFace(vcount + 3, vcount + 4, vcount);
        }
        private static void addPointsAndFace(List<Point3d> pts, ref Mesh m)
        {
            int vcount = m.Vertices.Count;
            m.Vertices.AddVertices(pts);
            if (pts.Count == 3)
            {
                m.Faces.AddFace(vcount, vcount + 1, vcount + 2);
            }
            else
            {
                m.Faces.AddFace(vcount, vcount + 1, vcount + 2, vcount + 3);
            }
        }
        private static List<Point3d> getFacePoints(MeshFace f, Mesh m)
        {
            List<Point3d> pts = new List<Point3d>();
            pts.Add(m.Vertices[f.A]);
            pts.Add(m.Vertices[f.B]);
            pts.Add(m.Vertices[f.C]);
            pts.Add(m.Vertices[f.D]);
            return pts;
        }
        private static bool pointsInsidePlane(List<Point3d> pts, Plane pln)
        {
            foreach (Point3d p in pts)
            {
                if (!pointInsidePlane(p, pln)) return false;
            }
            return true;
        }
        public static bool pointInsidePlane(Point3d p, Plane pln)
        {
            Vector3d v = p - pln.Origin;
            if (Vector3d.VectorAngle(v, pln.Normal) <= Math.PI / 2) return true;
            else return false;
        }


        public static void writeMeshes(List<Mesh> meshes, string path, string file)
        {
            StreamWriter results = new StreamWriter(path + "//" + file + ".js");
            foreach (Mesh m in meshes)
            {
                addMesh(results, m);
            }
            results.Close();
        }
        public static void writeMesh(Mesh mesh, string path, string file)
        {
            StreamWriter results = new StreamWriter(path + "//" + file + ".js");
            addMesh(results, mesh);

            results.Close();
        }
        private static void addMesh(StreamWriter results, Mesh mesh)
        {
            results.Write("var coords =[");
            for (int i = 0; i<mesh.Vertices.Count; i++)
            {
                if (i == mesh.Vertices.Count - 1)
                {
                    results.WriteLine(mesh.Vertices[i].X.ToString() + "," + mesh.Vertices[i].Y.ToString() + "," + mesh.Vertices[i].Z.ToString() + "];");
                }
                else
                {
                    results.Write(mesh.Vertices[i].X.ToString() + "," + mesh.Vertices[i].Y.ToString() + "," + mesh.Vertices[i].Z.ToString() + ",");
                }

            }
            results.Write("var faces=[");
            for (int i = 0; i<mesh.Faces.Count; i++)
            {
                if (i == mesh.Faces.Count - 1)
                {
                    results.WriteLine(mesh.Faces[i].A.ToString() + "," + mesh.Faces[i].B.ToString() + "," + mesh.Faces[i].C.ToString() + "," + mesh.Faces[i].D.ToString() + "];");
                }
                else
                {
                    results.Write(mesh.Faces[i].A.ToString() + "," + mesh.Faces[i].B.ToString() + "," + mesh.Faces[i].C.ToString() + "," + mesh.Faces[i].D.ToString() + ",");
                }


            }
            //results.Write("var colors=[");
            //for (int i = 0; i<mesh.VertexColors.Count; i++)
            //{
            //    if (i == mesh.VertexColors.Count - 1)
            //    {
            //        results.WriteLine(mesh.VertexColors[i].A.ToString() + "," + mesh.VertexColors[i].R.ToString() + "," + mesh.VertexColors[i].G.ToString() + "," + mesh.VertexColors[i].B.ToString() + "];");
            //    }
            //    else
            //    {
            //        results.Write(mesh.VertexColors[i].A.ToString() + "," + mesh.VertexColors[i].R.ToString() + "," + mesh.VertexColors[i].G.ToString() + "," + mesh.VertexColors[i].B.ToString() + ",");
            //    }

            //}
        }
        public static List<Mesh> readMesh(string meshfile)
        {
            List<Mesh> meshes = new List<Mesh>();
            Mesh mesh = new Mesh();
            StreamReader reader = new StreamReader(meshfile);
            string line = reader.ReadLine();
            while(line != null)
            {
                int start = line.IndexOf("[");
                int end = line.IndexOf("]");
                string values = line.Substring(start+1, end-start-1);
                string[] parts = values.Split(',');
                if (line.Contains("coords"))
                {
                    mesh = new Mesh();
                    for (int i = 0; i < parts.Length; i += 3)
                    {
                        try
                        {
                            Point3d p = new Point3d(Convert.ToDouble(parts[i]), Convert.ToDouble(parts[i + 1]), Convert.ToDouble(parts[i + 2]));
                            mesh.Vertices.Add(p);
                        }
                        catch
                        {
                            throw new Exception("Data format error");
                        }
                    }
                }
                if (line.Contains("faces"))
                {
                    for (int i = 0; i < parts.Length; i += 4)
                    {
                        try
                        {
                            mesh.Faces.AddFace(Convert.ToInt32(parts[i]), Convert.ToInt32(parts[i + 1]), Convert.ToInt32(parts[i + 2]), Convert.ToInt32(parts[i + 3]));
                        }
                        catch
                        {
                            throw new Exception("Data format error");
                        }
                    }
                    meshes.Add(mesh);
                }
                //if (line.Contains("colors"))
                //{
                //    for (int i = 0; i < parts.Length; i += 4)
                //    {
                //        try
                //        {
                //            mesh.VertexColors.Add(Convert.ToInt32(parts[i + 1]), Convert.ToInt32(parts[i + 2]), Convert.ToInt32(parts[i + 3]));
                //        }
                //        catch
                //        {
                //            throw new Exception("Data format error");
                //        }
                //    }
                //}
                line = reader.ReadLine();
            }
            reader.Close();
            return meshes;
        }
    }
    
}
