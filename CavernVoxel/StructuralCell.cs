using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace CavernVoxel
{
    class StructuralCell
    {
        public int[] id;
        public Mesh boundary;
        
        public Brep innerBoundary;
        public Brep trimInnerBoundary;
        
        public List<Curve> centreLines = new List<Curve>();
        public Mesh caveFace = new Mesh();
        public Brep millingVolume = new Brep();
        public CellType cellType;
        
        public List<Point3d> nodes = new List<Point3d>();
        public Point3d centroid;
        //plane normals point to out side mesh
        Plane midPlane;
        Plane frontPlane;
        Plane backPlane;
        Vector3d toOutside;
        double memberSize;
        public StructuralCell(Mesh bound, double memberDim,int[] ID)
        {
            boundary = bound;
            memberSize = memberDim;
            cellType = CellType.Undefined;
            id = ID;
            setInnerBound();
            getUntrimmedCentreLines();
        }
        public StructuralCell (Mesh bound,double memberDim,Mesh mesh, int[] ID)
        {
            cellType = CellType.SkinCell;
            
            boundary = bound;
            boundary.FaceNormals.ComputeFaceNormals();
            memberSize = memberDim;
            caveFace = mesh;
            id = ID;
            setInnerBound();
            trimCell();
        }
        
        private void trimCell()
        {
            setMidPlane();
            getBackFrontPlanes();
            trimStructure();
            findNodes();
            //trimBoundary = splitHalfSpace(frontPlane, boundary);
            millingVolume = trimOutMillingVolume();
        }
        private void setInnerBound()
        {
            centroid = MeshTools.meshCentroid(boundary);
            Mesh offset = boundary.Offset(memberSize/2);
            innerBoundary = Brep.CreateFromMesh(offset, false);
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
        private void findNodes()
        {
            Brep outerBoundary = Brep.CreateFromMesh(boundary, false);
            Plane trim = new Plane(backPlane.Origin, backPlane.Normal);
            var result = outerBoundary.Trim(trim, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            if (result.Length > 0)
            {
                
                foreach (BrepEdge e in result[0].Edges)
                {
                    if (e.Valence == EdgeAdjacency.Naked)
                    {
                        nodes.Add(e.PointAtStart);
                        nodes.Add(e.PointAtEnd);
                    }

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
        private void getUntrimmedCentreLines()
        {
            foreach (BrepEdge e in innerBoundary.Edges)
            {
                centreLines.Add(e.DuplicateCurve());

            }
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
                    //angle is greate than 90 so its behind
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
        
        
        
    }
}
