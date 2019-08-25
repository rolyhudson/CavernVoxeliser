using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino;
using System.IO;
using Rhino.Display;

namespace CavernVoxel
{
    class MeshVoxeliser
    {

        List<Mesh> meshesToVoxelise;
        
        public List<StructuralSpan> structuralSpans = new List<StructuralSpan>();
        public VoxelParameters parameters;
        public Text3d sectionNum;
        Plane gridPlane;
        double length;
        
        
        Mesh baseCell = new Mesh();
        public MeshVoxeliser(List<Mesh> meshes,double x, double y,double z,double memberDim, int startBay, int endBay,bool explore, Plane refPlane)
        {
            parameters = new VoxelParameters(x, y, z, memberDim, explore,startBay);
            meshesToVoxelise = meshes;
            meshesToVoxelise.ForEach(m => m.Normals.ComputeNormals());
            meshesToVoxelise.ForEach(m => m.FaceNormals.ComputeFaceNormals());
            gridPlane = refPlane;
            setText(startBay);
            //findBBox();
            setupSpans(startBay, endBay);
            VoxelDocumenter.moduleSchedule(this);
        }
        private void setText(int num)
        {
            Plane txtPln = new Plane(gridPlane.Origin+gridPlane.XAxis* parameters.width,gridPlane.XAxis,gridPlane.YAxis);
            string sNum = num.ToString();
            if (num < 10) sNum = "0" + sNum;
            sectionNum = new Text3d(sNum,txtPln,1000);
        }
        private void setupSpans(int start, int end)
        {
            int unitsY = end - start;
            if (start % 2 != 0) start = start - 1;
            if (end > unitsY) end = unitsY;
            if (start > unitsY) { start = unitsY - 1; end = unitsY; }
            
            for (int y = start; y < end; y+=2)//unitsY
            {
                Vector3d shiftY = gridPlane.YAxis * y * parameters.yCell;
                Point3d basePt = new Point3d(gridPlane.OriginX, gridPlane.OriginY, gridPlane.OriginZ);
                Point3d origin = basePt + shiftY;
                Plane boxPln = new Plane(origin, gridPlane.XAxis, gridPlane.YAxis);
                //containing box with allowance in x and z directions
                Box box = new Box(boxPln, new Interval(-parameters.xCell / 10, parameters.width + parameters.xCell / 10), new Interval(0, parameters.yCell * 2), new Interval(-parameters.zCell / 10, parameters.height + parameters.zCell / 10));
                Mesh sectionVolume = Mesh.CreateFromBox(box, 1, 1, 1);
                Mesh slice = MeshTools.splitMeshWithMesh(meshesToVoxelise[0], sectionVolume);
                if (slice != null)
                {
                    structuralSpans.Add(new StructuralSpan(parameters,slice,boxPln,y));
                }
            }
        }
        private void findBBox()
        {
            BoundingBox minBBox = new BoundingBox();
            Plane minBBoxPln = new Plane();
            double minBBoxRot = 0;
            if (gridPlane == null)
            {
                minBBox = findBBoxByPlanRotation(ref minBBoxPln,ref minBBoxRot);
            }
            else
            {
                minBBox = findBBoxGivenPlane(gridPlane);
                minBBoxPln = gridPlane;
                minBBoxRot = Vector3d.VectorAngle(Vector3d.XAxis, gridPlane.XAxis);
            }
            parameters.width = minBBox.Max.X - minBBox.Min.X;
            length = minBBox.Max.Y - minBBox.Min.Y;
            parameters.height = minBBox.Max.Z - minBBox.Min.Z;
            //Transform xform = Transform.PlaneToPlane(Plane.WorldXY, minBBoxPln);
            //Point3d origin = minBBox.Min;
            //origin.Transform(xform);
            //gridPlane = new Plane(origin, Vector3d.ZAxis);
            //gridPlane.Rotate(minBBoxRot, Vector3d.ZAxis);
        }
        private BoundingBox findBBoxGivenPlane(Plane pln)
        {
            List<Point3d> pts = new List<Point3d>();
            foreach (Mesh m in meshesToVoxelise)
            {
                foreach (Point3d p in m.Vertices)
                {
                    Point3d remapped = new Point3d();
                    pln.RemapToPlaneSpace(p, out remapped);

                    pts.Add(remapped);
                }
            }
            BoundingBox bBox = new BoundingBox(pts);
            return bBox;
        }
        private BoundingBox findBBoxByPlanRotation(ref Plane minBBoxPln,ref double minBBoxRot)
        {
            BoundingBox minBBox = new BoundingBox();
            double minVol = Double.MaxValue;
            double theta = Math.PI / 100;
           
            for (int i = 0; i < 100; i++)
            {
                Plane pln = Plane.WorldXY;
                pln.Rotate(theta * i, Vector3d.ZAxis);
                
                BoundingBox bBox = findBBoxGivenPlane(pln);
                if (bBox.Volume < minVol)
                {
                    minBBoxPln = pln;
                    minVol = bBox.Volume;
                    minBBox = bBox;
                    minBBoxRot = theta * i;
                }

            }
            return minBBox;
            
        }
        
    }
}
