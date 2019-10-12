using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace CavernVoxel
{
    class StructuralCell
    {
        public string id;
        public Mesh boundary;
        public Brep outerBoundary;
        public Brep innerBoundary;
        public Brep trimInnerBoundary;
        public List<Curve> untrimmedCentreLines = new List<Curve>();
        public List<Curve> centreLines = new List<Curve>();
        public List<Curve> diagonals = new List<Curve>();

        public Mesh caveFace = new Mesh();
        public Brep millingVolume = new Brep();
        public CellType cellType;
        public Mesh GSAmesh;
        public List<Point3d> nodes = new List<Point3d>();
        public Point3d centroid;
        public bool fillerCell;
        public double caveFaceArea;
        //plane normals point to out side mesh
        public Plane midPlane;
        public Color displayColor;
        public int rowNum;
        public int colNum;
        public int side;
        public int bay;
        public int part;
        public Curve boundCurve0;
        public Curve boundCurve1;
        public Plane boundPlane0;
        public Plane boundPlane1;
        public double zDim;
        public double yDim;
        public double xDim;
        public Plane cellPlane;
        Plane frontPlane;
        Plane backPlane;
        Vector3d toOutside;
        double memberSize;
        List<DiagonalMember> diagonalMembers = new List<DiagonalMember>();
        public List<Point3d> basePoints = new List<Point3d>();
        List<Point3d> nodeGrid = new List<Point3d>();
        public StructuralCell(Plane cellplane,double xdim,double ydim,double zdim, double memberDim,string ID,bool filler,Color c)
        {
            cellPlane = cellplane;
            xDim = xdim;
            yDim = ydim;
            zDim = zdim;
            boundary = MeshTools.makeCuboid(cellPlane,xDim,yDim,zDim);
            boundary.FaceNormals.ComputeFaceNormals();
            memberSize = memberDim;
            
            cellType = CellType.Undefined;
            id = ID;
            setPositionFromID();
            fillerCell = filler;
            displayColor = c;
            setInnerBound();
            centreLines = untrimmedCentreLines;
            storeDiagonals();
        }
        public void setSkinCell(Mesh mesh)
        {
            caveFace = mesh;
            if (caveFace.Faces.Count == 0)
            {
                cellType = CellType.Undefined;
            }
            else
            {
                cellType = CellType.SkinCell;
                trimCell();
                storeDiagonals();
                setFaceArea();
            }
        }
        private void setPositionFromID()
        {
            string[] parts = id.Split('_');
            rowNum = Convert.ToInt32(parts[4]);
            colNum = Convert.ToInt32(parts[3]);
            side = Convert.ToInt32(parts[2]);
            bay = Convert.ToInt32(parts[1]);
            part = Convert.ToInt32(parts[0]);
        }

        private void setColor()
        {
            Random r = new Random();
            int red = r.Next(255);
            int green = r.Next(255);
            int blue = r.Next(255);
            displayColor = Color.FromArgb(red, green, blue);
        }
        
        private void setFaceArea()
        {
            AreaMassProperties mp = AreaMassProperties.Compute(caveFace);
            if (mp != null) caveFaceArea = mp.Area;
        }
        private void trimCell()
        {

            setMidPlane();
            //getBackFrontPlanes();
            //trimStructure();
            findNodesTrimCentreLines();
            intersectDiagonals();
        }
        private void setInnerBound()
        {
            centroid = MeshTools.meshCentroid(boundary);
            Mesh offset = MeshTools.makeCuboid(cellPlane, xDim - memberSize, yDim - memberSize, zDim - memberSize);
            innerBoundary = Brep.CreateFromMesh(offset, false);
            
            foreach(BrepVertex p in innerBoundary.Vertices)
            {
                nodeGrid.Add(p.Location);
            }
            nodeGrid.Add(new Line(nodeGrid[0], nodeGrid[2]).PointAt(0.5));
            nodeGrid.Add(new Line(nodeGrid[1], nodeGrid[3]).PointAt(0.5));
            nodeGrid.Add(new Line(nodeGrid[6], nodeGrid[4]).PointAt(0.5));
            nodeGrid.Add(new Line(nodeGrid[7], nodeGrid[5]).PointAt(0.5));

            setCentreLines();
            //foreach(BrepEdge be in innerBoundary.Edges)
            //{
            //    untrimmedCentreLines.Add(be.DuplicateCurve());
            //}
            ////add diagonals

            for (int d = 0; d < 11; d++) diagonalMembers.Add(new DiagonalMember(d, nodeGrid));
            //set the faceboundaries
            setBoundaryGeometry();
        }
        private void setCentreLines()
        {
           
            //base
            untrimmedCentreLines.Add(new Line(nodeGrid[0], nodeGrid[8]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[1], nodeGrid[9]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[2], nodeGrid[8]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[3], nodeGrid[9]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[2], nodeGrid[3]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[0], nodeGrid[1]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[8], nodeGrid[9]).ToNurbsCurve());
            //top
            untrimmedCentreLines.Add(new Line(nodeGrid[4], nodeGrid[10]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[5], nodeGrid[11]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[6], nodeGrid[10]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[7], nodeGrid[11]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[4], nodeGrid[5]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[6], nodeGrid[7]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[10], nodeGrid[11]).ToNurbsCurve());
            //verticals
            untrimmedCentreLines.Add(new Line(nodeGrid[0], nodeGrid[6]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[1], nodeGrid[7]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[2], nodeGrid[4]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[3], nodeGrid[5]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[8], nodeGrid[10]).ToNurbsCurve());
            untrimmedCentreLines.Add(new Line(nodeGrid[9], nodeGrid[11]).ToNurbsCurve());
        }
        private void setBoundaryGeometry()
        {
            outerBoundary = Brep.CreateFromMesh(boundary, false);
            boundCurve0 = outerBoundary.Faces[2].OuterLoop.To3dCurve();
            boundCurve1 = outerBoundary.Faces[4].OuterLoop.To3dCurve();
            boundPlane0 = new Plane(outerBoundary.Faces[2].PointAt(0.5, 0.5), outerBoundary.Faces[2].NormalAt(0.5, 0.5));
            boundPlane1 = new Plane(outerBoundary.Faces[4].PointAt(0.5, 0.5), outerBoundary.Faces[4].NormalAt(0.5, 0.5));
            basePoints.Add(outerBoundary.Vertices[0].Location);
            basePoints.Add(outerBoundary.Vertices[1].Location);
            basePoints.Add(outerBoundary.Vertices[2].Location);
            basePoints.Add(outerBoundary.Vertices[3].Location);
        }
        private Brep trimOutMillingVolume()
        {
            var result = innerBoundary.Trim(frontPlane, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            Brep millingVol = new Brep();
            if (result.Length > 0)
            {
                millingVol = result[0];
                //flip the back plane
                Plane trim = new Plane(backPlane.Origin, backPlane.Normal);
                trim.Flip();
                var result2 = millingVol.Trim(trim, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                if (result2.Length > 0)
                {
                    millingVol = result2[0];
                }
            }
            Brep capped =   millingVol.CapPlanarHoles(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            return capped;
        }
        private void intersectDiagonals()
        {
            for(int c=0;c<diagonalMembers.Count;c++)
            {
                diagonalMembers[c].trim(caveFace);
            }
        }
        private void storeDiagonals()
        {
            //reset the diagonal collection
            diagonals = new List<Curve>();
            for (int c = 0; c < diagonalMembers.Count; c++)
            {
                if (diagonalMembers[c].needed)
                {
                    diagonals.Add(diagonalMembers[c].diagonal.ToNurbsCurve());
                }
            }
        }
        private void findNodesTrimCentreLines()
        {
            //reset thecentreline collection
            centreLines = new List<Curve>();
            foreach(Curve c in untrimmedCentreLines)
            {
                
                Line edge = new Line(c.PointAtStart, c.PointAtEnd);
                int[] faceIds;
                Point3d[] points = Rhino.Geometry.Intersect.Intersection.MeshLine(caveFace, edge, out faceIds);
                if (points.Length > 0)
                {
                    foreach (Point3d p in points)
                    {
                        Plane trimPln = new Plane(p, midPlane.Normal);
                        centreLines.Add(splitLineHalfSpace(trimPln, edge).ToNurbsCurve());
                        nodes.Add(p);
                    }
                }
                else
                {
                    //untrimmed centre line 
                    //is it inside or outside the mesh
                    if(!curveIsInsideMesh(c, caveFace))centreLines.Add(c);
                }
            }
            if(nodes.Count>2) MakeGSAMesh();
        }
        public static bool curveIsInsideMesh(Curve c, Mesh m)
        {
            //mesh normals towards inside
            Point3d mid = c.PointAt(c.Domain.Mid);
            MeshPoint mp = m.ClosestMeshPoint(mid, 0);
            Vector3d v = mid - mp.Point;
            if (Vector3d.VectorAngle(v,m.FaceNormals[mp.FaceIndex]) < Math.PI / 2)
            {
                //inside
                return true;
            }
            return false;
        }
        private void MakeGSAMesh()
        {
            GSAmesh = new Mesh();
            //convert point3d to node2
            //grasshopper requres that nodes are saved within a Node2List for Delaunay
            var node2s = new Grasshopper.Kernel.Geometry.Node2List();
            for (int i = 0; i < nodes.Count; i++)
            {

                //map nodes onto the mid plane
                Point3d mappedPt = new Point3d();
                midPlane.RemapToPlaneSpace(nodes[i], out mappedPt);
                node2s.Append(new Grasshopper.Kernel.Geometry.Node2(mappedPt.X, mappedPt.Y));
                GSAmesh.Vertices.Add(nodes[i]);
            }
            //solve Delaunay
            var delMesh = new Mesh();
            var faces = new List<Grasshopper.Kernel.Geometry.Delaunay.Face>();

            faces = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Faces(node2s,1);

            //output in 2d
            delMesh = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Mesh(node2s, 1, ref faces);
            if (delMesh != null)
            {
                foreach (MeshFace f in delMesh.Faces)
                {
                    GSAmesh.Faces.AddFace(f);
                }
            }
            
        }
        private void trimStructure()
        {
            Plane trim = new Plane(backPlane.Origin, backPlane.Normal);
            
            var result = innerBoundary.Trim(trim, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            if (result.Length > 0)
            {
                trimInnerBoundary = result[0];
                foreach(BrepEdge e in trimInnerBoundary.Edges)
                {
                    centreLines.Add(e.DuplicateCurve());
                }
            }

        }
        private Brep splitBrep(Plane pln,Brep toSplit)
        {
            var result = toSplit.Trim(pln, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            return result[0];
        }
        
        
        private void setMidPlane()
        {
            List<Point3d> facePts = new List<Point3d>();
            //plane normals follow caveface
            foreach(Point3d p in caveFace.Vertices)
            {
                facePts.Add(p);
            }
            foreach (Vector3d mf in caveFace.FaceNormals)
            {
                toOutside += mf;
            }

            Point3d planeOrigin = MeshTools.averagePoint(facePts);
            toOutside.Unitize();
            Plane.FitPlaneToPoints(facePts, out midPlane);
            midPlane.Origin = planeOrigin;
            if (Vector3d.VectorAngle(midPlane.Normal, toOutside) > Math.PI / 2)
            {
                midPlane.Flip();
            }
        }
        private void getBackFrontPlanes()
        {
            double maxDistInfront = 0;
            double maxDistBehind = 0;
            
            foreach (Point3d p in caveFace.Vertices)
            {
                Point3d onPlane = midPlane.ClosestPoint(p);
                //vector is perpendicular o plane
                Vector3d v = p - onPlane;
                //ignoring any point on the plane
                if (Vector3d.VectorAngle(midPlane.Normal, v) > Math.PI / 2)
                {
                    //angle is greater than 90 so its behind
                    if (v.Length > maxDistBehind) maxDistBehind = v.Length;
                }
                else
                {
                    if (v.Length > maxDistInfront) maxDistInfront = v.Length;
                }
            }
            
            
            Point3d infront = midPlane.Origin + (midPlane.Normal * maxDistInfront);
            frontPlane = new Plane(midPlane.Origin,midPlane.Normal);
            frontPlane.Origin = infront;
            Point3d behind = midPlane.Origin + (midPlane.Normal * -maxDistBehind);
            backPlane = new Plane(midPlane.Origin, midPlane.Normal);
            backPlane.Origin = behind;
        }
        public enum CellType
        {
            SkinCell,
            PerimeterCell,
            VerticalFillCell,
            InsideCell,
            Undefined
        }
        private Line splitLineHalfSpace(Plane pln, Line line)
        {
            double p = 0;
            var res = Rhino.Geometry.Intersect.Intersection.LinePlane(line, pln, out p);
            if (res)
            {
                Point3d iPt = line.PointAt(p);
                Vector3d v = line.From - iPt;
                if (Vector3d.VectorAngle(pln.Normal, v) > Math.PI / 2)
                {
                    return new Line(iPt, line.From);
                }
                else
                {
                    return new Line(iPt, line.To);
                }
            }
            else
            {
                return line;
            }
        }


    }
}
