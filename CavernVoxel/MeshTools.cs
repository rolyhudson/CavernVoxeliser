using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace CavernVoxel
{
    class MeshTools
    {
        public static Mesh splitMeshWithMesh(Mesh meshToSplit, Mesh closedSplitter)
        {
            var splits = meshToSplit.Split(closedSplitter);

            if (splits != null)
            {
                if (splits.Count() > 0)
                {
                    //assuming the contained part has fewer vertices than the rest of the cavern
                    if (splits[0].Vertices.Count > splits[1].Vertices.Count)
                    {
                        return splits[1];
                    }
                    else
                    {
                        return splits[0];
                    }
                }

            }
            return null;
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
        private Mesh splitMeshWithPlanes(Mesh toSplit,Mesh splitter)
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
                trimmedMesh = splitHalfSpace(facePlane, trimmedMesh);
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
        
        private Mesh splitHalfSpace(Plane pln, Mesh mesh)
        {
            var splits = mesh.Split(pln);
            foreach (Mesh m in splits)
            {
                Point3d furthest = new Point3d();
                double maxDist = 0;
                foreach (Point3d p in m.Vertices)
                {
                    Point3d closest = pln.ClosestPoint(p);
                    if (closest.DistanceTo(p) > maxDist)
                    {
                        furthest = p;
                        maxDist = closest.DistanceTo(p);
                    }
                }

                Vector3d v = furthest - pln.Origin;
                if (Vector3d.VectorAngle(pln.Normal, v) < Math.PI / 2)
                {
                    return m;
                }
            }
            return mesh;
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
